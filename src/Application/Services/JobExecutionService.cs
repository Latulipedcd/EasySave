using Core.Enums;
using Core.Interfaces;
using Core.Models;
using EasySave.Application.Interfaces;
using Log.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Application.Services
{
    /// <summary>
    /// Executes backup jobs and provides runtime control (pause/resume/stop).
    /// Moved out of JobManagementService to keep responsibilities small.
    /// </summary>
    public class JobExecutionService : IJobExecutionService
    {
        private readonly IUserConfigRepository _userConfigService;
        private readonly IBackupJobRepository _backupJobRepository;
        private readonly IBackupService _backupService;
        private readonly IProgressWriter _progressWriter;
        private readonly IBusinessSoftwareMonitor _businessSoftwareMonitor;
        private readonly ILanguageService _languageService;

        private readonly ConcurrentDictionary<string, JobExecutionHandle> _runningJobs = new();
        private CancellationTokenSource? _monitorCts;

        public JobExecutionService(
            IUserConfigRepository userConfigService,
            IBackupJobRepository backupJobRepository,
            IBackupService backupService,
            IProgressWriter progressWriter,
            IBusinessSoftwareMonitor businessSoftwareMonitor,
            ILanguageService languageService)
        {
            _userConfigService = userConfigService ?? throw new ArgumentNullException(nameof(userConfigService));
            _backupJobRepository = backupJobRepository ?? throw new ArgumentNullException(nameof(backupJobRepository));
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            _progressWriter = progressWriter ?? throw new ArgumentNullException(nameof(progressWriter));
            _businessSoftwareMonitor = businessSoftwareMonitor ?? throw new ArgumentNullException(nameof(businessSoftwareMonitor));
            _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
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

            _progressWriter.Clear();

            var logFormat = _userConfigService.LoadLogFormat() ?? LogFormat.Json;
            var storageMode = _userConfigService.LoadStorageMode() ?? LogStorageMode.Local;
            var businessSoftware = _userConfigService.LoadBusinessSoftware();
            var cryptoExtensions = _userConfigService.LoadCryptoSoftExtensions() ?? new List<string>();
            var cryptoPath = GetCryptoSoftPath();
            var priorityExtensions = _userConfigService.LoadPriorityExtensions() ?? new List<string>();
            var maxParallelFileSizeKb = _userConfigService.LoadMaxParallelFileSizeKb();

            var handles = new List<JobExecutionHandle>();
            foreach (var job in jobsToExecute)
            {
                var handle = new JobExecutionHandle(job.Name);
                _runningJobs[job.Name] = handle;
                handles.Add(handle);
            }

            StartBusinessSoftwareMonitor(businessSoftware);

            using var executionContext = new SharedExecutionContext(priorityExtensions, maxParallelFileSizeKb);

            var results = await Task.Run(async () =>
            {
                for (int i = 0; i < handles.Count; i++)
                {
                    var capturedJob = jobsToExecute[i];
                    var capturedHandle = handles[i];
                    capturedHandle.ExecutionTask = _backupService.ExecuteBackupAsync(
                        capturedJob,
                        logFormat,
                        cryptoExtensions,
                        cryptoPath,
                        capturedHandle.Cts.Token,
                        capturedHandle.PauseEvent,
                        executionContext,
                        storageMode);
                }
                return await Task.WhenAll(handles.Select(h => h.ExecutionTask!));
            });

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

        private void StopBusinessSoftwareMonitor()
        {
            _monitorCts?.Cancel();
            _monitorCts?.Dispose();
            _monitorCts = null;
        }

        // Helpers copied from JobManagementService
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

        private static string? GetCryptoSoftPath()
        {
            string workDir = AppDomain.CurrentDomain.BaseDirectory;
            string cryptoPath = Path.Combine(workDir, "Resources", "CryptoSoft.exe");
            return File.Exists(cryptoPath) ? cryptoPath : null;
        }
    }
}
