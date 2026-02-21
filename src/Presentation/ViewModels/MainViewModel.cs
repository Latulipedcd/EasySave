using Core.Interfaces;
using Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasySave.Presentation.ViewModels
{
    /// <summary>
    /// Presentation-layer orchestrator: initialises the specialised ViewModels
    /// and exposes async operations for the GUI.
    /// Receives all dependencies via constructor injection from the composition root.
    /// </summary>
    public class MainViewModel
    {
        private readonly ILanguageService _languageService;

        // Specialised ViewModels
        public SettingsViewModel Settings { get; }
        public JobEditorViewModel JobEditor { get; }
        public JobListViewModel JobList { get; }
        public JobExecutionViewModel JobExecution { get; }

        public MainViewModel(
            ILanguageService languageService,
            IUserConfigService userConfigService,
            IBackupJobRepository backupJobRepository,
            IJobManagementService jobManagementService)
        {
            _languageService = languageService;
            Settings = new SettingsViewModel(languageService, userConfigService);
            JobEditor = new JobEditorViewModel();
            JobList = new JobListViewModel(backupJobRepository, languageService);
            JobExecution = new JobExecutionViewModel(jobManagementService, languageService);
        }

        public string GetText(string key) => _languageService.GetString(key);

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
        /// Refreshes inline execution indicators in the jobs list.
        /// </summary>
        public void RefreshJobListExecutionState()
        {
            JobList.UpdateExecutionStates(JobExecution.LatestProgressStates);
        }

        /// <summary>
        /// Clears the current job execution state
        /// </summary>
        public void ClearJobState()
        {
            JobExecution.ClearJobState();
            JobList.UpdateExecutionStates(null);
        }
    }
}
