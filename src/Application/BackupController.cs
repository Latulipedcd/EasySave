using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using EasySave.Application.Configuration;
using Log.Enums;
using Log.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasySave.Application;

/// <summary>
/// Application-layer controller that wires up infrastructure services and exposes
/// all use-case operations for backup job management.
/// Used directly by the Console layer and as the backbone of the Presentation layer.
/// </summary>
public class BackupController
{
    private readonly LanguageManager _langManager;
    private readonly UserConfigManager _userConfigManager;
    private readonly IBackupJobRepository _backupJobRepository;
    private readonly IBackupService _backupService;

    // Exposed for the Presentation layer to initialise its specialised ViewModels
    public LanguageManager Language => _langManager;
    public UserConfigManager UserConfig => _userConfigManager;
    public IBackupJobRepository JobRepository => _backupJobRepository;
    public IBackupService BackupService => _backupService;

    public BackupController()
    {
        _langManager = LanguageManager.GetInstance();
        _userConfigManager = new UserConfigManager();
        _backupJobRepository = new BackupJobRepository(new JobStorage());
        _backupService = new BackupService(
            LogService.Instance,
            new FileService(),
            new CopyService(),
            new ProgressJsonWriter(),
            new BusinessSoftwareMonitor());
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
            message = GetText("JobCreatedSuccess");
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
                message = GetText("ErrorJobNotFound");
                return false;
            }
            _backupJobRepository.Delete(jobs[jobId - 1].Name);
            message = GetText("JobDeletedSuccess");
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
                message = GetText("ErrorJobNotFound");
                return false;
            }
            var existingJob = jobs[jobId - 1];
            BackupType trueJobType = jobType == 0 ? BackupType.Full : BackupType.Differencial;
            _backupJobRepository.Update(new BackupJob(existingJob.Name, newSrcPath, newTargetPath, trueJobType));
            message = GetText("JobUpdatedSuccess");
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
        results = new List<BackupState>();
        errorMessage = string.Empty;

        var jobs = _backupJobRepository.GetAll().ToList();
        var jobsToExecute = ResolveJobsFromInput(userInput, jobs, out errorMessage);
        if (jobsToExecute == null)
            return false;

        foreach (var job in jobsToExecute)
        {
            var state = _backupService.ExecuteBackup(
                job,
                GetSavedLogFormat(),
                GetSavedBusinessSoftware(),
                GetCryptoSoftExtensions(),
                GetCryptoSoftPath());
            results.Add(state);
        }

        return true;
    }

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
                    errorMessage = GetText("ErrorInvalidRange");
                    return null;
                }
                result.AddRange(allJobs.Skip(startId - 1).Take(endId - startId + 1));
            }
            else
            {
                if (!int.TryParse(part, out int jobId) || jobId < 1 || jobId > allJobs.Count)
                {
                    errorMessage = GetText("ErrorJobNotFound");
                    return null;
                }
                result.Add(allJobs[jobId - 1]);
            }
        }

        return result.Distinct().ToList();
    }

    public bool TryLoadSavedLanguage()
    {
        string? savedLanguage = _userConfigManager.LoadLanguage();
        if (string.IsNullOrWhiteSpace(savedLanguage))
            return false;
        return _langManager.LoadLanguage(savedLanguage);
    }

    public bool ChangeLanguage(string cultureCode)
    {
        bool loaded = _langManager.LoadLanguage(cultureCode);
        if (!loaded)
            return false;
        _userConfigManager.SaveLanguage(_langManager.CurrentCultureCode);
        return true;
    }

    public bool ChangeLogFormat(string format)
    {
        if (format == "Json") { _userConfigManager.SaveLogFormat(LogFormat.Json); return true; }
        if (format == "Xml")  { _userConfigManager.SaveLogFormat(LogFormat.Xml);  return true; }
        return false;
    }

    public bool ChangeBusinessSoftware(string software)
        => _userConfigManager.SaveBusinessSoftware(software);

    public bool ChangeCryptoSoftExtensions(List<string> extensions)
        => _userConfigManager.SaveCryptoSoftExtensions(extensions);

    public LogFormat GetSavedLogFormat()
        => _userConfigManager.LoadLogFormat() ?? LogFormat.Json;

    public string? GetSavedBusinessSoftware()
        => _userConfigManager.LoadBusinessSoftware();

    public List<string> GetCryptoSoftExtensions()
        => _userConfigManager.LoadCryptoSoftExtensions() ?? new List<string>();

    public string? GetCryptoSoftPath()
    {
        string workDir = AppDomain.CurrentDomain.BaseDirectory;
        string cryptoPath = Path.Combine(workDir, "Resources", "CryptoSoft.exe");
        return File.Exists(cryptoPath) ? cryptoPath : null;
    }

    public IReadOnlyList<string> GetSupportedLanguages()
        => _langManager.GetSupportedLanguages();

    public string CurrentLanguageCode => _langManager.CurrentCultureCode;

    public string GetText(string key) => _langManager.GetString(key);
}
