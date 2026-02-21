using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Enums;
using Core.Interfaces;
using Core.Models;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// ViewModel responsible for job execution and progress monitoring
/// </summary>
public class JobExecutionViewModel : ViewModelBase
{
    private readonly IJobManagementService _jobManagementService;
    private readonly ILanguageService _langManager;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Current job being monitored
    /// </summary>
    private BackupJob? _monitoredJob;
    public BackupJob? MonitoredJob
    {
        get => _monitoredJob;
        set
        {
            if (SetProperty(ref _monitoredJob, value))
            {
                RefreshJobState();
            }
        }
    }

    /// <summary>
    /// Execution state of the monitored job
    /// </summary>
    private BackupState? _jobState;
    public BackupState? JobState
    {
        get => _jobState;
        private set
        {
            if (SetProperty(ref _jobState, value))
            {
                OnPropertyChanged(nameof(IsJobRunning));
                OnPropertyChanged(nameof(ShowExecutionDetails));
                OnPropertyChanged(nameof(IsJobCompleted));
                OnPropertyChanged(nameof(IsJobPaused));
                OnPropertyChanged(nameof(JobProgress));
                OnPropertyChanged(nameof(JobTotalFiles));
                OnPropertyChanged(nameof(JobFilesRemaining));
                OnPropertyChanged(nameof(JobTotalSize));
                OnPropertyChanged(nameof(JobSizeRemaining));
                OnPropertyChanged(nameof(JobCurrentFile));
                OnPropertyChanged(nameof(JobLastUpdate));
                StateChanged?.Invoke(this, _jobState);
            }
        }
    }

    /// <summary>
    /// Indicates if the job is currently running
    /// </summary>
    public bool IsJobRunning => JobState != null && JobState.Status == BackupStatus.Active;

    /// <summary>
    /// Indicates if the job is currently paused
    /// </summary>
    public bool IsJobPaused => JobState != null && JobState.Status == BackupStatus.Paused;

    /// <summary>
    /// Indicates if execution details should be shown
    /// </summary>
    public bool ShowExecutionDetails => JobState != null &&
        (JobState.Status == BackupStatus.Active ||
         JobState.Status == BackupStatus.Paused ||
         JobState.Status == BackupStatus.Completed ||
         JobState.Status == BackupStatus.Error ||
         JobState.Status == BackupStatus.Cancelled);

    /// <summary>
    /// Indicates if job has completed (success, error, or cancelled)
    /// </summary>
    public bool IsJobCompleted => JobState != null &&
        (JobState.Status == BackupStatus.Completed ||
         JobState.Status == BackupStatus.Error ||
         JobState.Status == BackupStatus.Cancelled);

    // Job execution properties
    public double JobProgress => JobState?.ProgressPercentage ?? 0;
    public string JobTotalFiles => JobState?.TotalFiles.ToString() ?? "-";
    public string JobFilesRemaining => JobState?.FilesRemaining.ToString() ?? "-";
    public string JobTotalSize => FormatBytes(JobState?.TotalBytes ?? 0);
    public string JobSizeRemaining => FormatBytes(JobState?.BytesRemaining ?? 0);
    public string JobCurrentFile => JobState?.CurrentFileSource ?? "-";
    public string JobLastUpdate => JobState?.TimeStamp.ToString("HH:mm:ss") ?? "-";

    /// <summary>
    /// All execution states read from state.json (for list-level monitoring).
    /// Contains one entry per running/completed job in the current batch.
    /// </summary>
    public List<BackupState>? LatestProgressStates { get; private set; }

    /// <summary>
    /// Event raised when job state changes
    /// </summary>
    public event EventHandler<BackupState?>? StateChanged;

    public JobExecutionViewModel(
        IJobManagementService jobManagementService,
        ILanguageService languageService)
    {
        _jobManagementService = jobManagementService;
        _langManager = languageService;
    }

    /// <summary>
    /// Executes all jobs in parallel
    /// </summary>
    public async Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteAllJobsAsync(int totalJobCount)
    {
        if (totalJobCount == 0)
        {
            return (false, new List<BackupState>(), _langManager.GetString("GuiErrorNoJobToExecute"));
        }

        var input = $"1-{totalJobCount}";
        return await ExecuteJobsByInputAsync(input);
    }

    /// <summary>
    /// Executes selected jobs in parallel
    /// </summary>
    public async Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteSelectedJobsAsync(
        IReadOnlyList<BackupJob> selectedJobs,
        Func<BackupJob, int> getJobId)
    {
        if (selectedJobs == null || selectedJobs.Count == 0)
        {
            return (false, new List<BackupState>(), _langManager.GetString("GuiErrorNoJobSelected"));
        }

        var ids = selectedJobs
            .Select(job => getJobId(job))
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (ids.Count == 0)
        {
            return (false, new List<BackupState>(), _langManager.GetString("GuiErrorInvalidSelection"));
        }

        var input = string.Join(';', ids);
        return await ExecuteJobsByInputAsync(input);
    }

    /// <summary>
    /// Executes jobs specified by input string (e.g., "1-5" or "1;3;5") in parallel
    /// </summary>
    public async Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteJobsByInputAsync(string input)
    {
        return await _jobManagementService.ExecuteBackupJobsAsync(input);
    }

    /// <summary>
    /// Pauses a running job by name.
    /// </summary>
    public void PauseJob(string jobName) => _jobManagementService.PauseJob(jobName);

    /// <summary>
    /// Resumes a paused job by name.
    /// </summary>
    public void ResumeJob(string jobName) => _jobManagementService.ResumeJob(jobName);

    /// <summary>
    /// Stops (cancels) a running job by name.
    /// </summary>
    public void StopJob(string jobName) => _jobManagementService.StopJob(jobName);

    /// <summary>
    /// Pauses all currently running jobs.
    /// </summary>
    public void PauseAllJobs() => _jobManagementService.PauseAllJobs();

    /// <summary>
    /// Resumes all currently paused jobs.
    /// </summary>
    public void ResumeAllJobs() => _jobManagementService.ResumeAllJobs();

    /// <summary>
    /// Stops (cancels) all currently running jobs.
    /// </summary>
    public void StopAllJobs() => _jobManagementService.StopAllJobs();

    /// <summary>
    /// Refreshes the state of the monitored job
    /// Reads from state.json file created by ProgressJsonWriter
    /// </summary>
    public void RefreshJobState()
    {
        var states = ReadAllStates();
        LatestProgressStates = states;

        if (MonitoredJob == null || states == null || states.Count == 0)
        {
            JobState = null;
            return;
        }

        var matchingState = states.FirstOrDefault(s =>
            string.Equals(s.Job?.Name, MonitoredJob.Name, StringComparison.Ordinal));

        JobState = matchingState;
    }

    /// <summary>
    /// Clears the current job state
    /// </summary>
    public void ClearJobState()
    {
        JobState = null;
        LatestProgressStates = null;
    }

    /// <summary>
    /// Formats bytes to human-readable format (KB, MB, GB)
    /// </summary>
    private string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:0.##} {suffixes[suffixIndex]}";
    }

    /// <summary>
    /// Reads all progress states from state.json.
    /// The file contains a JSON array of BackupState objects (one per job).
    /// Returns null if unavailable or invalid.
    /// </summary>
    private List<BackupState>? ReadAllStates()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var statePath = Path.Combine(appData, "EasyLog", "Progress", "state.json");

            if (!File.Exists(statePath))
                return null;

            using var fs = new FileStream(statePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<List<BackupState>>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading state.json: {ex.Message}");
            return null;
        }
    }
}
