using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using EasySave.Application.Configuration;
using Log.Enums;
using Log.Services;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace EasySave.Application.ViewModels
{
    public class MainViewModel
    {
        private readonly LanguageManager _langManager;
        private readonly UserConfigManager _userConfigManager;
        private readonly IBackupJobRepository _backupJobRepository;
        private readonly IBackupService _backupService;

        public MainViewModel()
        {
            _langManager = LanguageManager.GetInstance();
            _userConfigManager = new UserConfigManager();
            _backupJobRepository= new BackupJobRepository(new JobStorage());
            _backupService= new BackupService(LogService.Instance, new FileService(), new CopyService(), new ProgressJsonWriter());
        }


        public IReadOnlyList<BackupJob> GetBackupJobs()
        {
            return _backupJobRepository.GetAll();
        }

        public bool CreateBackupJob(
         string jobTitle,
         string jobSrcPath,
         string jobTargetPath,
         int jobType,
         out string message)
        {
            try
            {
                //Change UI job type to a real value of the enum
                BackupType trueJobType = jobType == 0
                    ? BackupType.Full
                    : BackupType.Differencial;

                var job = new BackupJob(
                    jobTitle,
                    jobSrcPath,
                    jobTargetPath,
                    trueJobType);

                _backupJobRepository.Add(job); //Add job to repository

                message = GetText("JobCreatedSuccess");
                return true;
            }
            catch (InvalidOperationException ex)
            {
                message = ex.Message;
                return false;
            }
        }


        public bool DeleteBackupJob(int jobId, out string message) //Jobid is the display job id when job are display
        {
            try
            {
                var jobs = _backupJobRepository.GetAll();
                if (jobId < 1 || jobId > jobs.Count)
                {
                    message = GetText("ErrorJobNotFound");
                    return false;
                }

                var job = jobs[jobId - 1];
                _backupJobRepository.Delete(job.Name);

                message = GetText("JobDeletedSuccess");
                return true;
            }
            catch (InvalidOperationException ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public bool UpdateBackupJob(
    int jobId,
    string newSrcPath,
    string newTargetPath,
    int jobType,
    out string message)
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
                BackupType trueJobType = jobType == 0
                    ? BackupType.Full
                    : BackupType.Differencial;

                var updatedJob = new BackupJob(
                    existingJob.Name,
                    newSrcPath,
                    newTargetPath,
                    trueJobType
                );

                _backupJobRepository.Update(updatedJob);

                message = GetText("JobUpdatedSuccess");
                return true;
            }
            catch (InvalidOperationException ex)
            {
                message = ex.Message;
                return false;
            }
        }



        public bool ExecuteBackupJobs(
    string userInput,
    out List<BackupState> results,
    out string errorMessage)
        {
            results = new List<BackupState>();
            errorMessage = string.Empty;

            var jobs = _backupJobRepository.GetAll().ToList();

            var jobsToExecute = ResolveJobsFromInput(userInput, jobs, out errorMessage);
            if (jobsToExecute == null)
                return false;

            foreach (var job in jobsToExecute)
            {
                var state = _backupService.ExecuteBackup(job, GetSavedLogFormat());
                results.Add(state);
            }

            return true;
        }

        // Parses user input and resolves the corresponding backup jobs, differentiate - and ;
        private List<BackupJob>? ResolveJobsFromInput(
    string input,
    List<BackupJob> allJobs,
    out string errorMessage)
        {
            errorMessage = string.Empty;
            var result = new List<BackupJob>();

            var indexedJobs = allJobs;
            // Split input by ';' to support multiple selections
            var parts = input.Split(
                ';',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                // Handle range syntax (e.g. 1-3)
                if (part.Contains('-'))
                {
                    var range = part.Split(
                        '-',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (range.Length != 2)
                    {
                        errorMessage = GetText("ErrorInvalidRange");
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
                        errorMessage = GetText("ErrorInvalidRange");
                        return null;
                    }

                    result.AddRange(
                        indexedJobs.Skip(startId - 1).Take(endId - startId + 1));
                }
                else
                {
                    // Handle single job ID

                    bool isIdValid = int.TryParse(part, out int jobId);
                    if (!isIdValid || jobId < 1 || jobId > indexedJobs.Count)
                    {
                        errorMessage = GetText("ErrorJobNotFound");
                        return null;
                    }

                    result.Add(indexedJobs[jobId - 1]);
                }
            }
            // Remove duplicates and return final list
            return result.Distinct().ToList();
        }

        // Attempts to load the saved language from user configuration

        public bool TryLoadSavedLanguage()
        {
            string? savedLanguage = _userConfigManager.LoadLanguage();

            if (string.IsNullOrWhiteSpace(savedLanguage))
            {
                return false;
            }

            return _langManager.LoadLanguage(savedLanguage);
        }

        // Changes the current language and persists it to user configuration
        public bool ChangeLanguage(string cultureCode)
        {
            bool isLanguageLoaded = _langManager.LoadLanguage(cultureCode);
            if (!isLanguageLoaded)
            {
                return false;
            }

            _userConfigManager.SaveLanguage(_langManager.CurrentCultureCode);
            return true;
        }

        // Changes the current Log format in the user configuration
        public bool ChangeLogFormat(string format)
        {
            if (format == "Json")
            {
                _userConfigManager.SaveLogFormat(LogFormat.Json);
                return true;
            }
            else if (format == "Xml")
            {
                _userConfigManager.SaveLogFormat(LogFormat.Xml);
                return true;
            }
            else
            {
                return false;
            }
        }
            

        public LogFormat GetSavedLogFormat() 
        {
            return _userConfigManager.LoadLogFormat() ?? LogFormat.Json; // Get the saved log format or by default json
        }

        public IReadOnlyList<string> GetSupportedLanguages()
        {
            return _langManager.GetSupportedLanguages();
        }

        public string CurrentLanguageCode => _langManager.CurrentCultureCode;

        public string GetText(string key)
        {
            return _langManager.GetString(key);
        }
    }
}
