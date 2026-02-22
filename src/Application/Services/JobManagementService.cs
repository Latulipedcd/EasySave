using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Log.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Application.Services;

/// <summary>
/// Application-layer service that orchestrates backup job management.
/// Implements IJobManagementService from Core using proper dependency injection.
/// This service focuses ONLY on job-related operations (CRUD + Execution).
/// </summary>
public class JobManagementService : IJobManagementService
{
    private readonly ILanguageService _languageService;
    private readonly IUserConfigService _userConfigService;
    private readonly IBackupJobRepository _backupJobRepository;
    private readonly IBackupService _backupService;
    private readonly IBusinessSoftwareMonitor _businessSoftwareMonitor;
    private readonly IProgressWriter _progressWriter;

    /// <summary>
    /// Thread-safe dictionary keyed by job name that holds the handle for every
    /// currently running job. ConcurrentDictionary is used instead of Dictionary
    /// because the business software monitor thread reads it concurrently with the
    /// execution thread that adds/removes entries.
    /// </summary>
    private readonly ConcurrentDictionary<string, JobExecutionHandle> _runningJobs = new();

    /// <summary>
    /// CancellationTokenSource for the business software monitor loop.
    /// A separate CTS (not the per-job ones) so the monitor can be stopped
    /// independently once all jobs finish, without affecting the jobs themselves.
    /// </summary>
    private CancellationTokenSource? _monitorCts;

    /// <summary>
    /// Constructor with dependency injection.
    /// All dependencies are interfaces, making this class unit-testable.
    /// </summary>
    public JobManagementService(
        ILanguageService languageService,
        IUserConfigService userConfigService,
        IBackupJobRepository backupJobRepository,
        IBackupService backupService,
        IBusinessSoftwareMonitor businessSoftwareMonitor,
        IProgressWriter progressWriter)
    {
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _userConfigService = userConfigService ?? throw new ArgumentNullException(nameof(userConfigService));
        _backupJobRepository = backupJobRepository ?? throw new ArgumentNullException(nameof(backupJobRepository));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _businessSoftwareMonitor = businessSoftwareMonitor ?? throw new ArgumentNullException(nameof(businessSoftwareMonitor));
        _progressWriter = progressWriter ?? throw new ArgumentNullException(nameof(progressWriter));
    }

    public IReadOnlyList<BackupJob> GetBackupJobs()
        => _backupJobRepository.GetAll();

    public bool CreateBackupJob(string jobTitle, string jobSrcPath, string jobTargetPath, int jobType, out string message)
    {
        try
        {
            BackupType trueJobType = jobType == 0 ? BackupType.Full : BackupType.Differencial;
            var job = new BackupJob(jobTitle, jobSrcPath, jobTargetPath, trueJobType);
            _backupJobRepository.Add(job);
            message = _languageService.GetString("JobCreatedSuccess");
            return true;
        }
        catch (InvalidOperationException ex)
        {
            message = ex.Message;
            return false;
        }
    }

    public bool DeleteBackupJob(int jobId, out string message)
    {
        try
        {
            var jobs = _backupJobRepository.GetAll();
            if (jobId < 1 || jobId > jobs.Count)
            {
                message = _languageService.GetString("ErrorJobNotFound");
                return false;
            }
            _backupJobRepository.Delete(jobs[jobId - 1].Name);
            message = _languageService.GetString("JobDeletedSuccess");
            return true;
        }
        catch (InvalidOperationException ex)
        {
            message = ex.Message;
            return false;
        }
    }

    public bool UpdateBackupJob(int jobId, string newSrcPath, string newTargetPath, int jobType, out string message)
    {
        try
        {
            var jobs = _backupJobRepository.GetAll();
            if (jobId < 1 || jobId > jobs.Count)
            {
                message = _languageService.GetString("ErrorJobNotFound");
                return false;
            }
            var existingJob = jobs[jobId - 1];
            BackupType trueJobType = jobType == 0 ? BackupType.Full : BackupType.Differencial;
            _backupJobRepository.Update(new BackupJob(existingJob.Name, newSrcPath, newTargetPath, trueJobType));
            message = _languageService.GetString("JobUpdatedSuccess");
            return true;
        }
        catch (InvalidOperationException ex)
        {
            message = ex.Message;
            return false;
        }
    }

    public bool ExecuteBackupJobs(string userInput, out List<BackupState> results, out string errorMessage)
    {
        var (success, resultsList, error) = ExecuteBackupJobsAsync(userInput).GetAwaiter().GetResult();
        results = resultsList;
        errorMessage = error;
        return success;
    }

    public async Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteBackupJobsAsync(string userInput)
    {
        var jobs = _backupJobRepository.GetAll().ToList();
        var jobsToExecute = ResolveJobsFromInput(userInput, jobs, out string errorMessage);
        if (jobsToExecute == null)
            return (false, new List<BackupState>(), errorMessage);

        // Clear previous progress before starting a new batch
        _progressWriter.Clear();

        var logFormat = _userConfigService.LoadLogFormat() ?? LogFormat.Json;
        var businessSoftware = _userConfigService.LoadBusinessSoftware();
        var cryptoExtensions = _userConfigService.LoadCryptoSoftExtensions() ?? new List<string>();
        var cryptoPath = GetCryptoSoftPath();
        var priorityExtensions = _userConfigService.LoadPriorityExtensions() ?? new List<string>();
        var maxParallelFileSizeKb = _userConfigService.LoadMaxParallelFileSizeKb();

        // Create handles for each job
        var handles = new List<JobExecutionHandle>();
        foreach (var job in jobsToExecute)
        {
            var handle = new JobExecutionHandle(job.Name);
            _runningJobs[job.Name] = handle;
            handles.Add(handle);
        }

        // Start business software monitor on its own dedicated thread
        StartBusinessSoftwareMonitor(businessSoftware);

        // SharedExecutionContext holds the large-file semaphore and priority coordinator.
        // It is shared across all jobs in this batch and disposed once all jobs finish.
        using var executionContext = new SharedExecutionContext(priorityExtensions, maxParallelFileSizeKb);

        // Task.Run moves execution off the UI thread onto a thread pool thread so
        // the UI stays responsive while jobs run. The async lambda inside means the
        // thread pool thread itself is released while awaiting individual file operations.
        var results = await Task.Run(async () =>
        {
            for (int i = 0; i < handles.Count; i++)
            {
                var capturedJob = jobsToExecute[i];
                var capturedHandle = handles[i];
                // Start each job as an async Task (hot — starts immediately).
                // No Task.Run per job: they share the same async context and
                // cooperatively interleave via await Task.Yield() in ExecuteBackupAsync.
                capturedHandle.ExecutionTask = _backupService.ExecuteBackupAsync(
                    capturedJob,
                    logFormat,
                    cryptoExtensions,
                    cryptoPath,
                    capturedHandle.Cts.Token,
                    capturedHandle.PauseEvent,
                    executionContext);
            }
            // Task.WhenAll waits for ALL job tasks to complete (or fault/cancel).
            // It does not block a thread; it suspends this async method until every
            // task in the collection reaches a terminal state.
            return await Task.WhenAll(handles.Select(h => h.ExecutionTask!));
        });

        // Cleanup
        StopBusinessSoftwareMonitor();
        foreach (var handle in handles)
        {
            _runningJobs.TryRemove(handle.JobName, out _);
            handle.Dispose();
        }

        return (true, results.ToList(), string.Empty);
    }

    public void PauseJob(string jobName)
    {
        if (_runningJobs.TryGetValue(jobName, out var handle))
        {
            handle.ManuallyPaused = true;
            handle.UpdatePauseState();
        }
    }

    public void ResumeJob(string jobName)
    {
        if (_runningJobs.TryGetValue(jobName, out var handle))
        {
            handle.ManuallyPaused = false;
            handle.UpdatePauseState();
        }
    }

    public void StopJob(string jobName)
    {
        if (_runningJobs.TryGetValue(jobName, out var handle))
        {
            handle.Cts.Cancel();
        }
    }

    public void PauseAllJobs()
    {
        foreach (var handle in _runningJobs.Values)
        {
            handle.ManuallyPaused = true;
            handle.UpdatePauseState();
        }
    }

    public void ResumeAllJobs()
    {
        foreach (var handle in _runningJobs.Values)
        {
            handle.ManuallyPaused = false;
            handle.UpdatePauseState();
        }
    }

    public void StopAllJobs()
    {
        foreach (var handle in _runningJobs.Values)
        {
            handle.Cts.Cancel();
        }
    }

    /// <summary>
    /// Starts a background thread that periodically checks if business software is running.
    /// If detected, pauses all running jobs. When it exits, resumes them.
    /// </summary>
    private void StartBusinessSoftwareMonitor(string? businessSoftware)
    {
        if (string.IsNullOrEmpty(businessSoftware))
            return;

        _monitorCts = new CancellationTokenSource();
        var token = _monitorCts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                bool isRunning = _businessSoftwareMonitor.IsBusinessSoftwareRunning(businessSoftware);

                foreach (var handle in _runningJobs.Values)
                {
                    if (isRunning && !handle.BusinessPaused)
                    {
                        handle.BusinessPaused = true;
                        handle.UpdatePauseState();
                    }
                    else if (!isRunning && handle.BusinessPaused)
                    {
                        handle.BusinessPaused = false;
                        handle.UpdatePauseState();
                    }
                }

                try
                {
                    await Task.Delay(75, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    /// <summary>
    /// Stops the business software monitor thread.
    /// </summary>
    private void StopBusinessSoftwareMonitor()
    {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
    }

    /// <summary>
    /// Parses user input like "1;3-5" into a list of jobs to execute.
    /// </summary>
    private List<BackupJob>? ResolveJobsFromInput(string input, List<BackupJob> allJobs, out string errorMessage)
    {
        errorMessage = string.Empty;
        var result = new List<BackupJob>();
        var parts = input.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                var range = part.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (range.Length != 2
                    || !int.TryParse(range[0], out int startId)
                    || !int.TryParse(range[1], out int endId)
                    || startId < 1 || endId < 1
                    || startId > allJobs.Count || endId > allJobs.Count
                    || startId > endId)
                {
                    errorMessage = _languageService.GetString("ErrorInvalidRange");
                    return null;
                }
                result.AddRange(allJobs.Skip(startId - 1).Take(endId - startId + 1));
            }
            else
            {
                if (!int.TryParse(part, out int jobId) || jobId < 1 || jobId > allJobs.Count)
                {
                    errorMessage = _languageService.GetString("ErrorJobNotFound");
                    return null;
                }
                result.Add(allJobs[jobId - 1]);
            }
        }

        return result.Distinct().ToList();
    }

    /// <summary>
    /// Gets the path to CryptoSoft.exe if it exists.
    /// </summary>
    private static string? GetCryptoSoftPath()
    {
        string workDir = AppDomain.CurrentDomain.BaseDirectory;
        string cryptoPath = Path.Combine(workDir, "Resources", "CryptoSoft.exe");
        return File.Exists(cryptoPath) ? cryptoPath : null;
    }
}
