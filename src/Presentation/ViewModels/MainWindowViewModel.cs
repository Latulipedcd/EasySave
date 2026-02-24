using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Enums;
using Core.Models;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// DataContext for MainWindow. Provides cross-VM computed properties, localized
/// status text, feedback (CatSpeech / StatusMessage) and UI action methods.
/// All sub-ViewModels are exposed directly so the View can bind to their paths.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly BackupAppViewModel _appViewModel;

    public SettingsViewModel Settings => _appViewModel.Settings;
    public JobEditorViewModel JobEditor => _appViewModel.JobEditor;
    public JobListViewModel JobList => _appViewModel.JobList;
    public JobExecutionViewModel JobExecution => _appViewModel.JobExecution;

    private readonly System.Timers.Timer _stateRefreshTimer;
    private readonly SynchronizationContext? _uiContext;
    private string _catSpeech = string.Empty;

    private string Text(string key) => _appViewModel.GetText(key);
    private string TextFormat(string key, params object[] args) => string.Format(Text(key), args);

    public MainWindowTextsViewModel Texts { get; }

    /// <summary>
    /// Raised when the ViewModel decides a RunAll progress window should be opened.
    /// The view subscribes and calls ShowDialog; the ViewModel never touches the window.
    /// </summary>
    public event EventHandler<IReadOnlyList<BackupJobDisplayItem>>? RunAllWindowRequested;

    public string EditorWindowTitle => JobEditor.IsEditing
        ? Text("GuiEditorWindowTitleEdit")
        : Text("GuiEditorWindowTitleNew");

    public string GetText(string key) => Text(key);

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public MainWindowViewModel(BackupAppViewModel appViewModel)
    {
        _appViewModel = appViewModel;
        Texts = new MainWindowTextsViewModel(_appViewModel.GetText);

        Settings.LanguageChanged += (s, e) => RefreshLocalizedTexts();

        JobList.SelectionChanged += (s, job) =>
        {
            JobExecution.MonitoredJob = job;
            SetDefaultCatMessage();
            OnPropertyChanged(nameof(CanStartSelectedJob));
            OnPropertyChanged(nameof(CanStopSelectedJob));
            OnPropertyChanged(nameof(CanBreakSelectedJob));
        };

        JobExecution.StateChanged += (s, state) =>
        {
            OnPropertyChanged(nameof(JobStatusText));
            OnPropertyChanged(nameof(JobStatusColor));
            OnPropertyChanged(nameof(CanStartSelectedJob));
            OnPropertyChanged(nameof(CanStopSelectedJob));
            OnPropertyChanged(nameof(CanBreakSelectedJob));
        };

        JobEditor.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(JobEditor.EditingJobId) || e.PropertyName == nameof(JobEditor.IsEditing))
                OnPropertyChanged(nameof(EditorWindowTitle));
        };

        RefreshLocalizedTexts();

        _uiContext = SynchronizationContext.Current;
        _stateRefreshTimer = new System.Timers.Timer(500);
        _stateRefreshTimer.Elapsed += OnRefreshTimerTick;
        _stateRefreshTimer.AutoReset = true;
        _stateRefreshTimer.Start();

        _appViewModel.RefreshJobState();
        _appViewModel.RefreshJobListExecutionState();
    }

    private void OnRefreshTimerTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _uiContext?.Post(_ =>
        {
            _appViewModel.RefreshJobState();
            _appViewModel.RefreshJobListExecutionState();
        }, null);
    }

    private void RefreshLocalizedTexts()
    {
        Texts.Refresh();
        OnPropertyChanged(nameof(EditorWindowTitle));
        OnPropertyChanged(nameof(JobStatusText));
        SetDefaultCatMessage();

        if (Settings.SettingsItems.Count == 0)
            Settings.BuildSettingsMenuItems(
                Texts.LanguageLabel, Texts.LogFormatLabel,
                Texts.LogFormatJsonLabel, Texts.LogFormatXmlLabel,
                Texts.BusinessSoftwareLabel, Texts.CryptoExtensionsLabel,
                Texts.PriorityExtensionsLabel, Texts.MaxParallelFileSizeLabel);
        else
            Settings.RefreshSettingsMenuItems(
                Texts.LanguageLabel, Texts.LogFormatLabel,
                Texts.LogFormatJsonLabel, Texts.LogFormatXmlLabel,
                Texts.BusinessSoftwareLabel, Texts.CryptoExtensionsLabel,
                Texts.PriorityExtensionsLabel, Texts.MaxParallelFileSizeLabel);
    }

    // Cross-VM computed properties — require data from multiple sub-VMs
    public BackupJob? SelectedJob => JobList.SelectedJob;

    public string JobStatusText => JobExecution.JobState?.Status switch
    {
        BackupStatus.Inactive => Text("GuiStatusInactive"),
        BackupStatus.Active => Text("GuiStatusActive"),
        BackupStatus.Paused => Text("GuiStatusPaused"),
        BackupStatus.Completed => Text("GuiStatusCompleted"),
        BackupStatus.Error => Text("GuiStatusError"),
        BackupStatus.Cancelled => Text("GuiStatusCancelled"),
        _ => Text("GuiStatusInactive")
    };

    public string JobStatusColor => JobExecution.JobState?.Status switch
    {
        BackupStatus.Active => "#3498db",
        BackupStatus.Paused => "#f39c12",
        BackupStatus.Completed => "#27ae60",
        BackupStatus.Error => "#e74c3c",
        BackupStatus.Cancelled => "#e74c3c",
        _ => "#95a5a6"
    };

    public bool CanStartSelectedJob => JobList.HasSelection && !JobExecution.IsJobRunning;
    public bool CanStopSelectedJob => JobList.HasSelection && JobExecution.IsJobRunning;
    public bool CanBreakSelectedJob => JobList.HasSelection && (JobExecution.IsJobRunning || JobExecution.IsJobPaused);

    public string CatSpeech
    {
        get => _catSpeech;
        private set => SetProperty(ref _catSpeech, value);
    }

    private void SetCatMessage(string key, params object[] args)
    {
        CatSpeech = args.Length > 0 ? TextFormat(key, args) : Text(key);
    }

    private void SetCatRawMessage(string message) => CatSpeech = message;

    private void SetDefaultCatMessage()
    {
        if (JobList.HasSelection)
            SetCatMessage("GuiCatMessageSelected", JobList.SelectedJobName);
        else
            SetCatMessage("GuiCatMessageNoSelection");
    }

    public void ClearJobState() => _appViewModel.ClearJobState();

    public async Task CreateJobAsync()
    {
        SetCatMessage("GuiCatMessageCreating");

        var name = JobEditor.JobName;
        var (success, message) = await _appViewModel.CreateJobAsync();

        SetCatRawMessage(message);

        if (!success)
        {
            if (message != Text("GuiErrorJobNameEmpty"))
                SetCatMessage("GuiCatMessageActionFailed", message);
            return;
        }

        SetCatMessage("GuiCatMessageCreated", name);

        var jobs = await _appViewModel.RefreshJobsAsync();
        _appViewModel.ReplaceJobs(jobs);
    }

    public void SelectJob(BackupJobDisplayItem? item)
    {
        JobList.SelectedJob = item?.Job;
    }

    public void LoadSelectionForEdit(BackupJobDisplayItem item)
    {
        _appViewModel.LoadJobForEdit(item.Job);
        if (JobList.GetJobId(item.Job) > 0)
            SetCatMessage("GuiStatusEditModeHint");
    }

    public void ClearSelectionForEdit() => _appViewModel.ClearJobEditor();

    public async Task UpdateSelectedJobAsync()
    {
        SetCatMessage("GuiCatMessageUpdating");

        var (success, message, canContinue) = await _appViewModel.UpdateJobAsync();

        if (!canContinue)
        {
            if (message == Text("GuiErrorJobNameEmpty"))
                SetCatRawMessage(message);
            else
                SetStatusOnUIThread(message);
            return;
        }

        SetCatRawMessage(message);

        if (!success)
        {
            SetCatRawMessage(message);
            return;
        }

        SetCatMessage("GuiCatMessageUpdated", JobList.SelectedJobName);

        var jobs = await _appViewModel.RefreshJobsAsync();
        _appViewModel.ReplaceJobs(jobs);
    }

    public async Task DeleteSelectedJobsAsync(IReadOnlyList<BackupJobDisplayItem> selectedItems)
    {
        if (selectedItems == null || selectedItems.Count == 0)
        {
            SetStatusOnUIThread(Text("GuiErrorNoJobSelected"));
            SetDefaultCatMessage();
            return;
        }

        var selectedJobs = selectedItems.Select(i => i.Job).ToList();

        SetStatusOnUIThread(Text("GuiStatusDeleting"));
        SetCatMessage("GuiCatMessageDeleting");

        var (deletedCount, errors) = await _appViewModel.DeleteJobsAsync(selectedJobs);

        var jobs = await _appViewModel.RefreshJobsAsync();
        _appViewModel.ReplaceJobs(jobs);

        if (errors.Count == 0)
        {
            SetStatusOnUIThread(TextFormat("GuiStatusDeletedCount", deletedCount));
            SetCatMessage("GuiCatMessageDeleted", deletedCount);
            return;
        }

        SetStatusOnUIThread(TextFormat("GuiStatusDeletedWithErrors", deletedCount, errors.Count, errors[0]));
        SetCatMessage("GuiCatMessageDeleteWithErrors", errors.Count);
    }

    public async Task<bool> MoveJobAsync(BackupJobDisplayItem movedItem, BackupJobDisplayItem? targetItem)
    {
        return await JobList.MoveJobAsync(movedItem.Job, targetItem?.Job);
    }

    /// <summary>
    /// Builds the ViewModel for the delete confirmation dialog.
    /// Keeps name extraction and message formatting out of the view.
    /// </summary>
    public DeleteConfirmationViewModel CreateDeleteConfirmationViewModel(
        IReadOnlyList<BackupJobDisplayItem> selectedItems)
    {
        var names = selectedItems
            .Select(i => i.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();

        return new DeleteConfirmationViewModel(names, Text);
    }

    public async Task StartSelectedJobAsync()
    {
        var selectedJob = SelectedJob;
        if (!JobList.HasSelection || selectedJob == null)
        {
            SetStatusOnUIThread(Text("GuiErrorNoJobSelected"));
            SetDefaultCatMessage();
            return;
        }

        if (JobExecution.IsJobPaused)
        {
            JobExecution.ResumeJob(selectedJob.Name);
            SetStatusOnUIThread(Text("GuiStatusJobResumed"));
            SetCatRawMessage(Text("GuiStatusJobResumed"));
            return;
        }

        if (JobExecution.IsJobRunning)
            return;

        var displayItem = JobList.DisplayJobs.FirstOrDefault(d => d.Job == selectedJob);
        if (displayItem == null)
            return;

        await ExecuteSelectedAsync(new[] { displayItem });
    }

    public void StopSelectedJob()
    {
        var selectedJob = SelectedJob;
        if (!JobList.HasSelection || selectedJob == null || !JobExecution.IsJobRunning)
            return;

        JobExecution.PauseJob(selectedJob.Name);
        SetStatusOnUIThread(Text("GuiStatusJobPaused"));
        SetCatRawMessage(Text("GuiStatusJobPaused"));
    }

    public void BreakSelectedJob()
    {
        var selectedJob = SelectedJob;
        if (!JobList.HasSelection || selectedJob == null || (!JobExecution.IsJobRunning && !JobExecution.IsJobPaused))
            return;

        JobExecution.StopJob(selectedJob.Name);
        SetStatusOnUIThread(Text("GuiStatusJobCancelled"));
        SetCatRawMessage(Text("GuiStatusJobCancelled"));
    }

    public async Task ExecuteAllAsync() => await ExecuteAllWithResultAsync();

    /// <summary>
    /// Decides whether to run jobs directly (empty list) or signal the view to open
    /// the RunAll progress window. Moves the empty-list guard out of the code-behind.
    /// </summary>
    public async Task InitiateRunAllAsync()
    {
        var jobsToRun = JobList.DisplayJobs.ToList();
        if (jobsToRun.Count == 0)
        {
            await ExecuteAllAsync();
            return;
        }

        RunAllWindowRequested?.Invoke(this, jobsToRun);
    }

    public async Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteAllWithResultAsync()
    {
        SetStatusOnUIThread(Text("GuiStatusExecuting"));
        SetCatMessage("GuiCatMessageExecuting");

        var (success, results, errorMessage) = await _appViewModel.ExecuteAllJobsAsync();
        ApplyExecuteAllResult(success, results, errorMessage);
        return (success, results, errorMessage);
    }

    private void ApplyExecuteAllResult(bool success, List<BackupState> results, string errorMessage)
    {
        if (!success && JobList.BackupJobs.Count == 0)
        {
            StatusMessage = errorMessage;
            SetCatMessage("GuiCatMessageNoJobToRun");
            return;
        }

        if (!success)
        {
            SetStatusOnUIThread(errorMessage);
            SetCatMessage("GuiCatMessageActionFailed", errorMessage);
            return;
        }

        var completed = results.Count(r => r.Status == BackupStatus.Completed);
        var errors = results.Count(r => r.Status == BackupStatus.Error || r.Status == BackupStatus.Cancelled);
        SetStatusOnUIThread(TextFormat("GuiStatusExecutionSummary", results.Count, completed, errors));

        if (errors == 0)
            SetCatMessage("GuiCatMessageExecutedSuccess", completed);
        else
            SetCatMessage("GuiCatMessageExecutedWithErrors", errors);
    }

    public async Task ExecuteSelectedAsync(IReadOnlyList<BackupJobDisplayItem> selectedItems)
    {
        SetStatusOnUIThread(Text("GuiStatusExecuting"));
        SetCatMessage("GuiCatMessageExecuting");

        var selectedJobs = selectedItems?.Select(i => i.Job).ToList() ?? new List<BackupJob>();
        var (success, results, errorMessage) = await _appViewModel.ExecuteSelectedJobsAsync(selectedJobs);

        if (!success && selectedJobs.Count == 0)
        {
            StatusMessage = errorMessage;
            SetDefaultCatMessage();
            return;
        }

        if (!success)
        {
            SetStatusOnUIThread(errorMessage);
            SetCatMessage("GuiCatMessageActionFailed", errorMessage);
            return;
        }

        var completed = results.Count(r => r.Status == BackupStatus.Completed);
        var errors = results.Count(r => r.Status == BackupStatus.Error || r.Status == BackupStatus.Cancelled);
        SetStatusOnUIThread(TextFormat("GuiStatusExecutionSummary", results.Count, completed, errors));

        if (errors == 0)
            SetCatMessage("GuiCatMessageExecutedSuccess", completed);
        else
            SetCatMessage("GuiCatMessageExecutedWithErrors", errors);
    }

    private void SetStatusOnUIThread(string message)
    {
        if (_uiContext == null || SynchronizationContext.Current == _uiContext)
        {
            StatusMessage = message;
            return;
        }

        _uiContext.Post(_ => StatusMessage = message, null);
    }

    public void SetStatus(string message)
    {
        SetStatusOnUIThread(message);
        SetCatRawMessage(message);
    }
}
