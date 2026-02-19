using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Log.Enums;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// ViewModel responsible for job execution and progress monitoring
/// </summary>
public class JobExecutionViewModel : ViewModelBase
{
    private readonly IBackupService _backupService;
    private readonly IBackupJobRepository _backupJobRepository;
    private readonly IUserConfigService _userConfigManager;
    private readonly ILanguageService _langManager;

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
    /// Indicates if execution details should be shown
    /// </summary>
    public bool ShowExecutionDetails => JobState != null &&
        (JobState.Status == BackupStatus.Active ||
         JobState.Status == BackupStatus.Completed ||
         JobState.Status == BackupStatus.Error);

    /// <summary>
    /// Indicates if job has completed (success or error)
    /// </summary>
    public bool IsJobCompleted => JobState != null &&
        (JobState.Status == BackupStatus.Completed ||
         JobState.Status == BackupStatus.Error);

    // Job execution properties
    public double JobProgress => JobState?.ProgressPercentage ?? 0;
    public string JobTotalFiles => JobState?.TotalFiles.ToString() ?? "-";
    public string JobFilesRemaining => JobState?.FilesRemaining.ToString() ?? "-";
    public string JobTotalSize => FormatBytes(JobState?.TotalBytes ?? 0);
    public string JobSizeRemaining => FormatBytes(JobState?.BytesRemaining ?? 0);
    public string JobCurrentFile => JobState?.CurrentFileSource ?? "-";
    public string JobLastUpdate => JobState?.TimeStamp.ToString("HH:mm:ss") ?? "-";

    /// <summary>
    /// Event raised when job state changes
    /// </summary>
    public event EventHandler<BackupState?>? StateChanged;

    public JobExecutionViewModel(
        IBackupService backupService, 
        IBackupJobRepository backupJobRepository,
        IUserConfigService userConfigService,
        ILanguageService languageService)
    {
        _backupService = backupService;
        _backupJobRepository = backupJobRepository;
        _userConfigManager = userConfigService;
        _langManager = languageService;
    }

    /// <summary>
    /// Executes all jobs
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
    /// Executes selected jobs
    /// </summary>
    public async Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteSelectedJobsAsync(
        IReadOnlyList<BackupJob> selectedJobs,
        Func<BackupJob, int> getJobId)
    {
        if (selectedJobs == null || selectedJobs.Count == 0)
        {
            return (false, new List<BackupState>(), _langManager.GetString("GuiErrorNoJobSelected"));
        }

        // Convert jobs to IDs
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
    /// Executes jobs specified by input string (e.g., "1-5" or "1;3;5")
    /// </summary>
    public async Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteJobsByInputAsync(string input)
    {
        var (success, results, errorMessage) = await Task.Run(() =>
        {
            var resultsList = new List<BackupState>();
            string error = string.Empty;

            var jobs = _backupJobRepository.GetAll().ToList();
            var jobsToExecute = ResolveJobsFromInput(input, jobs, out error);
            
            if (jobsToExecute == null)
                return (false, resultsList, error);

            var logFormat = _userConfigManager.LoadLogFormat() ?? LogFormat.Json;
            var businessSoftware = _userConfigManager.LoadBusinessSoftware();
            var cryptoExtensions = _userConfigManager.LoadCryptoSoftExtensions() ?? new List<string>();
            var cryptoPath = GetCryptoSoftPath();

            foreach (var job in jobsToExecute)
            {
                var state = _backupService.ExecuteBackup(job, logFormat, businessSoftware, cryptoExtensions, cryptoPath);
                resultsList.Add(state);
            }

            return (true, resultsList, string.Empty);
        });

        return (success, results ?? new List<BackupState>(), errorMessage);
    }

    private List<BackupJob>? ResolveJobsFromInput(string input, List<BackupJob> allJobs, out string errorMessage)
    {
        errorMessage = string.Empty;
        var result = new List<BackupJob>();

        var indexedJobs = allJobs;
        var parts = input.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                var range = part.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (range.Length != 2)
                {
                    errorMessage = _langManager.GetString("ErrorInvalidRange");
                    return null;
                }

                if (!int.TryParse(range[0], out int startId)
                    || !int.TryParse(range[1], out int endId)
                    || startId < 1
                    || endId < 1
                    || startId > indexedJobs.Count
                    || endId > indexedJobs.Count
                    || startId > endId)
                {
                    errorMessage = _langManager.GetString("ErrorInvalidRange");
                    return null;
                }

                result.AddRange(indexedJobs.Skip(startId - 1).Take(endId - startId + 1));
            }
            else
            {
                bool isIdValid = int.TryParse(part, out int jobId);
                if (!isIdValid || jobId < 1 || jobId > indexedJobs.Count)
                {
                    errorMessage = _langManager.GetString("ErrorJobNotFound");
                    return null;
                }

                result.Add(indexedJobs[jobId - 1]);
            }
        }

        return result.Distinct().ToList();
    }

    private string? GetCryptoSoftPath()
    {
        string workDir = AppDomain.CurrentDomain.BaseDirectory;
        string cryptoPath = Path.Combine(workDir, "Resources", "CryptoSoft.exe");
        return File.Exists(cryptoPath) ? cryptoPath : null;
    }

    /// <summary>
    /// Refreshes the state of the monitored job
    /// Reads from state.json file created by ProgressJsonWriter
    /// </summary>
    public void RefreshJobState()
    {
        if (MonitoredJob == null)
        {
            JobState = null;
            return;
        }

        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var statePath = Path.Combine(appData, "EasyLog", "Progress", "state.json");

            if (!File.Exists(statePath))
            {
                JobState = null;
                return;
            }

            var json = File.ReadAllText(statePath);

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            var state = System.Text.Json.JsonSerializer.Deserialize<BackupState>(json, options);

            // Check if state corresponds to monitored job
            if (state?.Job?.Name == MonitoredJob.Name)
            {
                JobState = state;
            }
            else
            {
                JobState = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing job state: {ex.Message}");
            JobState = null;
        }
    }

    /// <summary>
    /// Clears the current job state
    /// </summary>
    public void ClearJobState()
    {
        JobState = null;
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
}
