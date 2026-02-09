using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using EasySave.Application.Configuration;
using System;
using System.Collections.Generic;

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
            _backupService= new BackupService(new FileService(), new CopyService());
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
                BackupType trueJobType = jobType == 0
                    ? BackupType.Full
                    : BackupType.Differencial;

                var job = new BackupJob(
                    jobTitle,
                    jobSrcPath,
                    jobTargetPath,
                    trueJobType);

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


        public bool DeleteBackupJob(string jobName, out string message)
        {
            try
            {
                _backupJobRepository.Delete(jobName);

                message = GetText("JobDeletedSuccess");
                return true;
            }
            catch (InvalidOperationException ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public bool BackupJobExists(string jobName)
        {
            var jobs = _backupJobRepository.GetAll();
            return jobs.Any(j => j.Name == jobName);
        }
        public bool UpdateBackupJob(
    string existingJobName,
    string newSrcPath,
    string newTargetPath,
    int jobType,
    out string message)
        {
            try
            {
                BackupType trueJobType = jobType == 0
                    ? BackupType.Full
                    : BackupType.Differencial;

                var updatedJob = new BackupJob(
                    existingJobName,
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
            if (jobs.Count == 0)
            {
                errorMessage = GetText("NoJobsFound");
                return false;
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                errorMessage = GetText("ErrorInvalidOption");
                return false;
            }

            var jobsToExecute = TryResolveJobsFromIndexInput(userInput, jobs, out errorMessage);
            if (jobsToExecute == null)
                return false;

            foreach (var job in jobsToExecute)
            {
                var state = _backupService.ExecuteBackup(job);
                results.Add(state);
            }

            return true;
        }
        private List<BackupJob>? TryResolveJobsFromIndexInput(
    string input,
    List<BackupJob> allJobs,
    out string errorMessage)
        {
            errorMessage = string.Empty;
            var result = new List<BackupJob>();
            var distinctIndexes = new HashSet<int>();
            var parts = input.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                return null;
            }

            if (parts.Any(part => !IsIndexSelectionPart(part)))
            {
                errorMessage = GetText("ErrorInvalidRange");
                return null;
            }

            foreach (var part in parts)
            {
                if (part.Contains('-'))
                {
                    var range = part.Split('-', StringSplitOptions.TrimEntries);
                    if (range.Length != 2
                        || !int.TryParse(range[0], out int start)
                        || !int.TryParse(range[1], out int end))
                    {
                        errorMessage = GetText("ErrorInvalidRange");
                        return null;
                    }

                    if (!IsValidJobIndex(start, allJobs.Count)
                        || !IsValidJobIndex(end, allJobs.Count)
                        || start > end)
                    {
                        errorMessage = GetText("ErrorInvalidRange");
                        return null;
                    }

                    for (int index = start; index <= end; index++)
                    {
                        if (distinctIndexes.Add(index))
                        {
                            result.Add(allJobs[index - 1]);
                        }
                    }
                }
                else
                {
                    if (!int.TryParse(part, out int index) || !IsValidJobIndex(index, allJobs.Count))
                    {
                        errorMessage = GetText("ErrorJobNotFound");
                        return null;
                    }

                    if (distinctIndexes.Add(index))
                    {
                        result.Add(allJobs[index - 1]);
                    }
                }
            }

            return result;
        }

        private static bool IsIndexSelectionPart(string part)
        {
            if (int.TryParse(part, out _))
            {
                return true;
            }

            var range = part.Split('-', StringSplitOptions.TrimEntries);
            return range.Length == 2
                && int.TryParse(range[0], out _)
                && int.TryParse(range[1], out _);
        }

        private static bool IsValidJobIndex(int index, int totalJobs)
        {
            return index >= 1 && index <= totalJobs;
        }


        public bool TryLoadSavedLanguage()
        {
            string? savedLanguage = _userConfigManager.LoadLanguage();

            if (string.IsNullOrWhiteSpace(savedLanguage))
            {
                return false;
            }

            return _langManager.LoadLanguage(savedLanguage);
        }

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

        public IReadOnlyList<string> GetSupportedLanguages()
        {
            return _langManager.GetSupportedLanguages();
        }

        public string GetText(string key)
        {
            return _langManager.GetString(key);
        }
    }
}
