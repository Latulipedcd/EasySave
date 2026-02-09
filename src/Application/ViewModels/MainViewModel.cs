using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using EasySave.Application.Configuration;
using System.Collections.Generic;
using Log.Services;

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

            var jobsToExecute = ResolveJobsFromInput(userInput, jobs, out errorMessage);
            if (jobsToExecute == null)
                return false;

            foreach (var job in jobsToExecute)
            {
                var state = _backupService.ExecuteBackup(job);
                results.Add(state);
            }

            return true;
        }


        private List<BackupJob>? ResolveJobsFromInput(
    string input,
    List<BackupJob> allJobs,
    out string errorMessage)
        {
            errorMessage = string.Empty;
            var result = new List<BackupJob>();

            var orderedJobs = allJobs.OrderBy(j => j.Name).ToList();

            var parts = input.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (part.Contains('-'))
                {
                    var range = part.Split('-', StringSplitOptions.RemoveEmptyEntries);
                    if (range.Length != 2)
                    {
                        errorMessage = GetText("ErrorInvalidRange");
                        return null;
                    }

                    var start = orderedJobs.FindIndex(j => j.Name == range[0]);
                    var end = orderedJobs.FindIndex(j => j.Name == range[1]);

                    if (start == -1 || end == -1 || start > end)
                    {
                        errorMessage = GetText("ErrorInvalidRange");
                        return null;
                    }

                    result.AddRange(orderedJobs.Skip(start).Take(end - start + 1));
                }
                else
                {
                    var job = orderedJobs.FirstOrDefault(j => j.Name == part);
                    if (job == null)
                    {
                        errorMessage = GetText("ErrorJobNotFound");
                        return null;
                    }

                    result.Add(job);
                }
            }

            return result.Distinct().ToList();
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
