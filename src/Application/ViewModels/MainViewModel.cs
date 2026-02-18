using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using EasySave.Application.Configuration;
using Log.Enums;
using Log.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasySave.Application.ViewModels
{
    /// <summary>
    /// Main ViewModel that orchestrates specialized ViewModels and provides backward compatibility
    /// </summary>
    public class MainViewModel
    {
        private readonly LanguageManager _langManager;
        private readonly UserConfigManager _userConfigManager;
        private readonly IBackupJobRepository _backupJobRepository;
        private readonly IBackupService _backupService;

        // Specialized ViewModels
        public SettingsViewModel Settings { get; }
        public JobEditorViewModel JobEditor { get; }
        public JobListViewModel JobList { get; }
        public JobExecutionViewModel JobExecution { get; }

        public MainViewModel()
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

            // Initialize specialized ViewModels
            Settings = new SettingsViewModel(_langManager, _userConfigManager);
            JobEditor = new JobEditorViewModel();
            JobList = new JobListViewModel(_backupJobRepository, _langManager);
            JobExecution = new JobExecutionViewModel(_backupService, _backupJobRepository, _userConfigManager, _langManager);
        }

        // Backward compatibility methods - delegate to specialized ViewModels

        public IReadOnlyList<BackupJob> GetBackupJobs() => JobList.BackupJobs;

        public bool CreateBackupJob(string jobTitle, string jobSrcPath, string jobTargetPath, int jobType, out string message)
        {
            var task = JobList.CreateJobAsync(jobTitle, jobSrcPath, jobTargetPath, jobType);
            task.Wait();
            var result = task.Result;
            message = result.message;
            return result.success;
        }

        public bool DeleteBackupJob(int jobId, out string message)
        {
            var jobs = JobList.BackupJobs;
            if (jobId < 1 || jobId > jobs.Count)
            {
                message = GetText("ErrorJobNotFound");
                return false;
            }

            var jobToDelete = jobs[jobId - 1];
            var task = JobList.DeleteJobsAsync(new[] { jobToDelete });
            task.Wait();
            var result = task.Result;
            
            message = result.errors.Count > 0 ? result.errors[0] : GetText("JobDeletedSuccess");
            return result.deletedCount > 0;
        }

        public bool UpdateBackupJob(int jobId, string newSrcPath, string newTargetPath, int jobType, out string message)
        {
            var task = JobList.UpdateJobAsync(jobId, newSrcPath, newTargetPath, jobType);
            task.Wait();
            var result = task.Result;
            message = result.message;
            return result.success;
        }

        public bool ExecuteBackupJobs(string userInput, out List<BackupState> results, out string errorMessage)
        {
            // Parse input to determine if it's "all" or specific jobs
            var jobs = JobList.BackupJobs;
            
            if (userInput.Contains('-'))
            {
                // Range or all jobs
                var task = JobExecution.ExecuteJobsByInputAsync(userInput);
                task.Wait();
                var result = task.Result;
                results = result.results;
                errorMessage = result.errorMessage;
                return result.success;
            }
            else
            {
                // Specific jobs by semicolon
                var task = JobExecution.ExecuteJobsByInputAsync(userInput);
                task.Wait();
                var result = task.Result;
                results = result.results;
                errorMessage = result.errorMessage;
                return result.success;
            }
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
            bool isLanguageLoaded = _langManager.LoadLanguage(cultureCode);
            if (!isLanguageLoaded)
                return false;

            _userConfigManager.SaveLanguage(_langManager.CurrentCultureCode);
            return true;
        }

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
            string cryptoPath = System.IO.Path.Combine(workDir, "Resources", "CryptoSoft.exe");
            return System.IO.File.Exists(cryptoPath) ? cryptoPath : null;
        }

        public IReadOnlyList<string> GetSupportedLanguages() 
            => _langManager.GetSupportedLanguages();

        public string CurrentLanguageCode => _langManager.CurrentCultureCode;

        public string GetText(string key) => _langManager.GetString(key);

        // Async methods for UI operations

        /// <summary>
        /// Creates a new backup job asynchronously
        /// </summary>
        public async Task<(bool success, string message)> CreateJobAsync()
        {
            if (string.IsNullOrWhiteSpace(JobEditor.JobName))
            {
                return (false, GetText("GuiErrorJobNameEmpty"));
            }

            var result = await JobList.CreateJobAsync(
                JobEditor.JobName,
                JobEditor.SourceDirectory,
                JobEditor.TargetDirectory,
                JobEditor.BackupTypeIndex);

            if (result.success)
            {
                JobEditor.ClearForm();
            }

            return result;
        }

        /// <summary>
        /// Loads a job into the editor for modification
        /// </summary>
        public void LoadJobForEdit(BackupJob job)
        {
            if (job == null)
            {
                JobEditor.ClearForm();
                return;
            }

            var id = JobList.GetJobId(job);
            if (id <= 0)
            {
                JobEditor.ClearForm();
                return;
            }

            JobEditor.LoadJobForEdit(job, id);
        }

        /// <summary>
        /// Clears the editor and exits edit mode
        /// </summary>
        public void ClearJobEditor()
        {
            JobEditor.ExitEditMode();
        }

        /// <summary>
        /// Updates the currently edited job with new values
        /// </summary>
        public async Task<(bool success, string message, bool canContinue)> UpdateJobAsync()
        {
            if (JobEditor.EditingJobId is null)
            {
                return (false, GetText("GuiErrorSelectSingleToEdit"), false);
            }

            if (string.IsNullOrWhiteSpace(JobEditor.JobName))
            {
                return (false, GetText("GuiErrorJobNameEmpty"), false);
            }

            var result = await JobList.UpdateJobAsync(
                JobEditor.EditingJobId.Value,
                JobEditor.JobName,
                JobEditor.SourceDirectory,
                JobEditor.TargetDirectory,
                JobEditor.BackupTypeIndex);

            if (result.success)
            {
                JobEditor.ExitEditMode();
            }

            return (result.success, result.message, true);
        }

        /// <summary>
        /// Deletes the specified jobs
        /// </summary>
        public async Task<(int deletedCount, List<string> errors)> DeleteJobsAsync(IReadOnlyList<BackupJob> selectedJobs)
        {
            var result = await JobList.DeleteJobsAsync(selectedJobs);
            JobEditor.ExitEditMode();
            return result;
        }

        /// <summary>
        /// Executes all backup jobs
        /// </summary>
        public async Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteAllJobsAsync()
        {
            var jobCount = JobList.BackupJobs.Count;
            if (jobCount == 0)
            {
                return (false, new List<BackupState>(), GetText("GuiErrorNoJobToExecute"));
            }

            return await JobExecution.ExecuteAllJobsAsync(jobCount);
        }

        /// <summary>
        /// Executes only the selected backup jobs
        /// </summary>
        public async Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteSelectedJobsAsync(
            IReadOnlyList<BackupJob> selectedJobs)
        {
            if (selectedJobs == null || selectedJobs.Count == 0)
            {
                return (false, new List<BackupState>(), GetText("GuiErrorNoJobSelected"));
            }

            return await JobExecution.ExecuteSelectedJobsAsync(selectedJobs, JobList.GetJobId);
        }

        /// <summary>
        /// Refreshes the job list and returns the updated list
        /// </summary>
        public async Task<IReadOnlyList<BackupJob>> RefreshJobsAsync()
        {
            return await JobList.RefreshJobsAsync();
        }

        /// <summary>
        /// Replaces the jobs collection with new data (to be called on UI thread)
        /// </summary>
        public void ReplaceJobs(IReadOnlyList<BackupJob> jobs)
        {
            JobList.ReplaceJobs(jobs);
        }

        /// <summary>
        /// Refreshes the current job state from state.json
        /// </summary>
        public void RefreshJobState()
        {
            JobExecution.RefreshJobState();
        }

        /// <summary>
        /// Clears the current job execution state
        /// </summary>
        public void ClearJobState()
        {
            JobExecution.ClearJobState();
        }
    }
}