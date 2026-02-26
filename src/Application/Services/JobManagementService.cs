using Core.Enums;
using Core.Interfaces;
using EasySave.Application.Interfaces;
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
    private readonly IUserConfigRepository _userConfigService;
    private readonly IBackupJobRepository _backupJobRepository;
    private readonly IBackupService _backupService;
    private readonly IBusinessSoftwareMonitor _businessSoftwareMonitor;
    private readonly IProgressWriter _progressWriter;
    private readonly EasySave.Application.Interfaces.IJobExecutionService _jobExecutionService;

    /// <summary>
    /// Constructor with dependency injection.
    /// All dependencies are interfaces, making this class unit-testable.
    /// </summary>
    public JobManagementService(
        ILanguageService languageService,
        IUserConfigRepository userConfigService,
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
        // Initialize execution service used for running/controlling jobs.
        _jobExecutionService = new JobExecutionService(
            _userConfigService,
            _backupJobRepository,
            _backupService,
            _progressWriter,
            _businessSoftwareMonitor,
            _languageService);
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
        return _jobExecutionService.ExecuteBackupJobs(userInput, out results, out errorMessage);
    }

    public async Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteBackupJobsAsync(string userInput)
    {
        return await _jobExecutionService.ExecuteBackupJobsAsync(userInput);
    }

    public void PauseJob(string jobName)
    {
        _jobExecutionService.PauseJob(jobName);
    }

    public void ResumeJob(string jobName)
    {
        _jobExecutionService.ResumeJob(jobName);
    }

    public void StopJob(string jobName)
    {
        _jobExecutionService.StopJob(jobName);
    }

    public void PauseAllJobs()
    {
        _jobExecutionService.PauseAllJobs();
    }

    public void ResumeAllJobs()
    {
        _jobExecutionService.ResumeAllJobs();
    }

    public void StopAllJobs()
    {
        _jobExecutionService.StopAllJobs();
    }

    }
