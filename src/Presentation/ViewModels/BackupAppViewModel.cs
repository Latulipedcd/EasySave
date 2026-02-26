using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Models;
using EasySave.Application.Interfaces;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// Application-level ViewModel that owns all specialised sub-ViewModels and wires
/// them together. MainWindowViewModel delegates all business operations here.
/// </summary>
public class BackupAppViewModel
{
    private readonly ILanguageService _langManager;

    public SettingsViewModel Settings { get; }
    public JobEditorViewModel JobEditor { get; }
    public JobListViewModel JobList { get; }
    public JobExecutionViewModel JobExecution { get; }

    public BackupAppViewModel(
        ILanguageService languageService,
        IUserConfigRepository userConfigService,
        IBackupJobRepository jobRepository,
        IJobManagementService jobManagementService,
        IJobStateReader jobStateReader,
        IProgressSnapshotSource? progressSnapshotSource = null)
    {
        _langManager = languageService;
        Settings = new SettingsViewModel(languageService, userConfigService);
        JobEditor = new JobEditorViewModel();
        JobList = new JobListViewModel(jobRepository, languageService);
        JobExecution = new JobExecutionViewModel(jobManagementService, languageService, jobStateReader, progressSnapshotSource);
    }

    public string GetText(string key) => _langManager.GetString(key);

    public async Task<(bool success, string message)> CreateJobAsync()
    {
        var name = JobEditor.JobName;
        if (string.IsNullOrWhiteSpace(name))
            return (false, GetText("GuiErrorJobNameEmpty"));

        return await JobList.CreateJobAsync(
            name,
            JobEditor.SourceDirectory,
            JobEditor.TargetDirectory,
            JobEditor.BackupTypeIndex);
    }

    public async Task<(bool success, string message, bool canContinue)> UpdateJobAsync()
    {
        var name = JobEditor.JobName;
        if (string.IsNullOrWhiteSpace(name))
            return (false, GetText("GuiErrorJobNameEmpty"), false);

        if (!JobEditor.EditingJobId.HasValue)
            return (false, GetText("GuiErrorNoJobSelected"), false);

        var (success, message) = await JobList.UpdateJobAsync(
            JobEditor.EditingJobId.Value,
            name,
            JobEditor.SourceDirectory,
            JobEditor.TargetDirectory,
            JobEditor.BackupTypeIndex);

        return (success, message, true);
    }

    public Task<(int deletedCount, List<string> errors)> DeleteJobsAsync(IReadOnlyList<BackupJob> jobs)
        => JobList.DeleteJobsAsync(jobs);

    public Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteAllJobsAsync()
        => JobExecution.ExecuteAllJobsAsync(JobList.BackupJobs.Count);

    public Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteSelectedJobsAsync(
        IReadOnlyList<BackupJob> selectedJobs)
        => JobExecution.ExecuteSelectedJobsAsync(selectedJobs, job => JobList.GetJobId(job));

    public Task<IReadOnlyList<BackupJob>> RefreshJobsAsync()
        => JobList.RefreshJobsAsync();

    public void ReplaceJobs(IReadOnlyList<BackupJob> jobs)
        => JobList.ReplaceJobs(jobs);

    public void RefreshJobState()
        => JobExecution.RefreshJobState();

    public void RefreshJobListExecutionState()
        => JobList.UpdateExecutionStates(JobExecution.LatestProgressStates);

    public void ClearJobState()
        => JobExecution.ClearJobState();

    public void LoadJobForEdit(BackupJob job)
        => JobEditor.LoadJobForEdit(job, JobList.GetJobId(job));

    public void ClearJobEditor()
        => JobEditor.ClearForm();
}
