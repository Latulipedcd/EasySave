using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Core.Enums;
using Core.Interfaces;
using Core.Models;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// ViewModel responsible for managing the backup jobs list and CRUD operations
/// </summary>
public class JobListViewModel : ViewModelBase
{
    private readonly IBackupJobRepository _backupJobRepository;
    private readonly ILanguageService _langManager;
    private readonly DateTime _sessionStartedAt = DateTime.Now;

    /// <summary>
    /// Observable collection of backup jobs
    /// </summary>
    public ObservableCollection<BackupJob> BackupJobs { get; }

    /// <summary>
    /// Observable collection of backup jobs for display with IDs
    /// </summary>
    public ObservableCollection<BackupJobDisplayItem> DisplayJobs { get; }

    /// <summary>
    /// Currently selected job
    /// </summary>
    private BackupJob? _selectedJob;
    public BackupJob? SelectedJob
    {
        get => _selectedJob;
        set
        {
            if (SetProperty(ref _selectedJob, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(SelectedJobName));
                OnPropertyChanged(nameof(SelectedJobSource));
                OnPropertyChanged(nameof(SelectedJobTarget));
                OnPropertyChanged(nameof(SelectedJobType));
                SelectionChanged?.Invoke(this, _selectedJob);
            }
        }
    }

    /// <summary>
    /// Indicates if a job is selected
    /// </summary>
    public bool HasSelection => SelectedJob != null;

    // Properties of the selected job
    public string SelectedJobName => SelectedJob?.Name ?? string.Empty;
    public string SelectedJobSource => SelectedJob?.SourceDirectory ?? string.Empty;
    public string SelectedJobTarget => SelectedJob?.TargetDirectory ?? string.Empty;
    public string SelectedJobType => SelectedJob?.Type.ToString() ?? string.Empty;

    /// <summary>
    /// Event raised when job selection changes
    /// </summary>
    public event EventHandler<BackupJob?>? SelectionChanged;

    public JobListViewModel(IBackupJobRepository backupJobRepository, ILanguageService languageService)
    {
        _backupJobRepository = backupJobRepository;
        _langManager = languageService;
        BackupJobs = new ObservableCollection<BackupJob>(_backupJobRepository.GetAll());
        DisplayJobs = new ObservableCollection<BackupJobDisplayItem>();
        RefreshDisplayJobs();
    }

    /// <summary>
    /// Refreshes the DisplayJobs collection based on BackupJobs
    /// </summary>
    private void RefreshDisplayJobs()
    {
        DisplayJobs.Clear();
        int id = 1;
        foreach (var job in BackupJobs)
        {
            DisplayJobs.Add(new BackupJobDisplayItem(job, id));
            id++;
        }
    }

    /// <summary>
    /// Updates inline execution state in the jobs list.
    /// Only the active job is highlighted with progress.
    /// </summary>
    public void UpdateExecutionState(BackupState? state)
    {
        foreach (var item in DisplayJobs)
        {
            // One job max can be active (state.json contains the latest one).
            item.IsRunning = false;
            item.ExecutionProgress = 0;
        }

        if (state?.Job == null)
            return;

        var target = DisplayJobs.FirstOrDefault(item =>
            string.Equals(item.Name, state.Job.Name, StringComparison.Ordinal));

        if (target == null)
            return;

        if ((state.Status == BackupStatus.Completed || state.Status == BackupStatus.Error) &&
            (state.TimeStamp == default || state.TimeStamp < _sessionStartedAt))
            return;

        switch (state.Status)
        {
            case BackupStatus.Active:
                target.IsRunning = true;
                target.ExecutionProgress = Math.Clamp(state.ProgressPercentage, 0, 100);
                target.IsCompletedSuccess = false;
                target.HasExecutionError = false;
                break;

            case BackupStatus.Completed:
                target.IsRunning = false;
                target.ExecutionProgress = 100;
                target.IsCompletedSuccess = true;
                target.HasExecutionError = false;
                break;

            case BackupStatus.Error:
                target.IsRunning = false;
                target.ExecutionProgress = 0;
                target.IsCompletedSuccess = false;
                target.HasExecutionError = true;
                break;
        }
    }

    /// <summary>
    /// Creates a new backup job
    /// </summary>
    public async Task<(bool success, string message)> CreateJobAsync(string name, string source, string target, int typeIndex)
    {
        var (success, message) = await Task.Run(() =>
        {
            try
            {
                BackupType trueJobType = typeIndex == 0 ? BackupType.Full : BackupType.Differencial;
                var job = new BackupJob(name, source, target, trueJobType);
                _backupJobRepository.Add(job);
                return (true, _langManager.GetString("JobCreatedSuccess"));
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }
        });

        return (success, message);
    }

    /// <summary>
    /// Updates an existing backup job
    /// </summary>
    public async Task<(bool success, string message)> UpdateJobAsync(int jobId, string source, string target, int typeIndex)
    {
        return await UpdateJobAsync(jobId, null, source, target, typeIndex);

    }

    // Surcharge avec nom
    public async Task<(bool success, string message)> UpdateJobAsync(int jobId, string? newName, string source, string target, int typeIndex)
    {
        var (success, message) = await Task.Run(() =>
        {
            try
            {
                var jobs = _backupJobRepository.GetAll();
                if (jobId < 1 || jobId > jobs.Count)
                {
                    return (false, _langManager.GetString("ErrorJobNotFound"));
                }

                var existingJob = jobs[jobId - 1];
                BackupType trueJobType = typeIndex == 0 ? BackupType.Full : BackupType.Differencial;
                string finalName = newName ?? existingJob.Name;

                // Si le nom a changé, supprimer l'ancien et ajouter le nouveau
                if (!string.Equals(existingJob.Name, finalName, StringComparison.Ordinal))
                {
                    _backupJobRepository.Delete(existingJob.Name);
                    var renamedJob = new BackupJob(finalName, source, target, trueJobType);
                    _backupJobRepository.Add(renamedJob);
                }
                else
                {
                    var updatedJob = new BackupJob(existingJob.Name, source, target, trueJobType);
                    _backupJobRepository.Update(updatedJob);
                }
                return (true, _langManager.GetString("JobUpdatedSuccess"));
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }
        });

        return (success, message);
    }

    /// <summary>
    /// Deletes selected jobs from the list
    /// </summary>
    public async Task<(int deletedCount, List<string> errors)> DeleteJobsAsync(IReadOnlyList<BackupJob> selectedJobs)
    {
        if (selectedJobs == null || selectedJobs.Count == 0)
        {
            return (0, new List<string>());
        }

        // Convert jobs to IDs, sort descending to delete from end to start
        var ids = selectedJobs
            .Select(job => BackupJobs.IndexOf(job) + 1)
            .Where(id => id > 0)
            .Distinct()
            .OrderByDescending(id => id)
            .ToList();

        var (deletedCount, errors) = await Task.Run(() =>
        {
            var deleted = 0;
            var errorMessages = new List<string>();
            
            foreach (var id in ids)
            {
                try
                {
                    var jobs = _backupJobRepository.GetAll();
                    if (id < 1 || id > jobs.Count)
                    {
                        errorMessages.Add(_langManager.GetString("ErrorJobNotFound"));
                        continue;
                    }

                    var job = jobs[id - 1];
                    _backupJobRepository.Delete(job.Name);
                    deleted++;
                }
                catch (InvalidOperationException ex)
                {
                    errorMessages.Add(ex.Message);
                }
            }

            return (deleted, errorMessages);
        });

        return (deletedCount, errors);
    }

    /// <summary>
    /// Gets the ID (1-based index) of a specific job
    /// </summary>
    public int GetJobId(BackupJob job)
    {
        return BackupJobs.IndexOf(job) + 1;
    }

    /// <summary>
    /// Refreshes the jobs list from storage
    /// Returns the updated list of jobs
    /// </summary>
    public async Task<IReadOnlyList<BackupJob>> RefreshJobsAsync()
    {
        return await Task.Run(() => _backupJobRepository.GetAll());
    }

    /// <summary>
    /// Moves a job before/at the target position and persists the new order
    /// </summary>
    public async Task<bool> MoveJobAsync(BackupJob movedJob, BackupJob? targetJob)
    {
        if (movedJob == null)
        {
            return false;
        }

        var sourceIndex = BackupJobs.IndexOf(movedJob);
        if (sourceIndex < 0)
        {
            return false;
        }

        var targetIndex = targetJob == null ? BackupJobs.Count - 1 : BackupJobs.IndexOf(targetJob);
        if (targetIndex < 0)
        {
            targetIndex = BackupJobs.Count - 1;
        }

        if (sourceIndex == targetIndex)
        {
            return false;
        }

        BackupJobs.Move(sourceIndex, targetIndex);
        RefreshDisplayJobs();

        await Task.Run(() => _backupJobRepository.ReplaceAll(BackupJobs.ToList()));
        return true;
    }

    /// <summary>
    /// Replaces the content of BackupJobs collection with new jobs
    /// Should be called on UI thread
    /// </summary>
    public void ReplaceJobs(IReadOnlyList<BackupJob> jobs)
    {
        BackupJobs.Clear();
        foreach (var job in jobs)
            BackupJobs.Add(job);
        
        RefreshDisplayJobs();
    }
}
