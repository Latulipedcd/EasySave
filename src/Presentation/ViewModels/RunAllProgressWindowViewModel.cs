using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Core.Enums;
using Core.Models;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// ViewModel for the dedicated window shown while running all jobs.
/// </summary>
public class RunAllProgressWindowViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly Dictionary<string, RunAllJobProgressItemViewModel> _jobRowsByName;
    private bool _executionStarted;
    private bool _suppressSelectionLock;
    private bool _selectionLockedByUser;

    public ObservableCollection<RunAllJobProgressItemViewModel> Jobs { get; }

    public string WindowTitle => Text("GuiRunAllWindowTitle");
    public string HeaderTitle => Text("GuiRunAllHeaderTitle");
    public string GlobalProgressLabel => Text("GuiRunAllGlobalProgress");
    public string JobsTitle => Text("GuiRunAllJobsTitle");
    public string CurrentJobTitle => Text("GuiRunAllCurrentJobTitle");
    public string CurrentJobEmpty => Text("GuiRunAllCurrentJobEmpty");
    public string CloseHint => Text("GuiRunAllCloseHint");
    public string SummaryPending => Text("GuiRunAllSummaryPending");
    public string SummaryRunning => Text("GuiRunAllSummaryRunning");
    public string SummaryCancelling => Text("GuiRunAllSummaryCancelling");
    public string CloseButtonLabel => Text("GuiRunAllCloseButton");
    public string StartButtonLabel => Text("GuiJobActionStart");
    public string PauseButtonLabel => Text("GuiJobActionStop");
    public string CancelButtonLabel => Text("GuiJobActionBreak");
    public string CloseConfirmTitle => Text("GuiRunAllCloseConfirmTitle");
    public string CloseConfirmMessage => Text("GuiRunAllCloseConfirmMessage");
    public string CloseConfirmCancelLabel => Text("GuiDeleteConfirmNo");
    public string CloseConfirmConfirmLabel => Text("GuiRunAllCloseConfirmConfirm");
    public string ColumnJobLabel => Text("GuiHeaderName");
    public string ColumnStatusLabel => Text("GuiRunAllColumnStatus");
    public string ColumnProgressLabel => Text("GuiRunAllColumnProgress");
    public string LabelSourceFolder => Text("GuiLabelSourceFolder");
    public string LabelDestinationFolder => Text("GuiLabelDestinationFolder");
    public string JobDetailsStatus => Text("GuiJobDetailsStatus");
    public string JobDetailsProgress => Text("GuiJobDetailsProgress");
    public string JobDetailsTotalFiles => Text("GuiJobDetailsTotalFiles");
    public string JobDetailsFilesRemaining => Text("GuiJobDetailsFilesRemaining");
    public string JobDetailsTotalSize => Text("GuiJobDetailsTotalSize");
    public string JobDetailsSizeRemaining => Text("GuiJobDetailsSizeRemaining");
    public string JobDetailsCurrentFile => Text("GuiJobDetailsCurrentFile");
    public string JobDetailsLastUpdate => Text("GuiJobDetailsLastUpdate");

    private RunAllJobProgressItemViewModel? _selectedJob;
    public RunAllJobProgressItemViewModel? SelectedJob
    {
        get => _selectedJob;
        set
        {
            if (!SetProperty(ref _selectedJob, value))
                return;

            if (!_suppressSelectionLock && value != null)
                _selectionLockedByUser = true;

            OnPropertyChanged(nameof(HasSelectedJob));
            OnPropertyChanged(nameof(CanStartSelectedJob));
            OnPropertyChanged(nameof(CanPauseSelectedJob));
            OnPropertyChanged(nameof(CanCancelSelectedJob));
        }
    }

    public bool HasSelectedJob => SelectedJob != null;
    public bool CanStartSelectedJob => IsRunning && SelectedJob?.Status == BackupStatus.Paused;
    public bool CanPauseSelectedJob => IsRunning && SelectedJob != null
        && (SelectedJob.Status == BackupStatus.Active || SelectedJob.Status == BackupStatus.Error);

    public bool CanCancelSelectedJob => IsRunning && SelectedJob != null
        && (SelectedJob.Status == BackupStatus.Active ||
            SelectedJob.Status == BackupStatus.Paused ||
            SelectedJob.Status == BackupStatus.Error);

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!SetProperty(ref _isRunning, value))
                return;

            OnPropertyChanged(nameof(CanStartSelectedJob));
            OnPropertyChanged(nameof(CanPauseSelectedJob));
            OnPropertyChanged(nameof(CanCancelSelectedJob));
        }
    }

    private bool _isExecutionCompleted;
    public bool IsExecutionCompleted
    {
        get => _isExecutionCompleted;
        private set => SetProperty(ref _isExecutionCompleted, value);
    }

    private double _globalProgress;
    public double GlobalProgress
    {
        get => _globalProgress;
        private set => SetProperty(ref _globalProgress, value);
    }

    private string _summaryMessage;
    public string SummaryMessage
    {
        get => _summaryMessage;
        private set => SetProperty(ref _summaryMessage, value);
    }

    public RunAllProgressWindowViewModel(
        MainWindowViewModel mainWindowViewModel,
        IReadOnlyList<BackupJobDisplayItem> jobsToRun)
    {
        _mainWindowViewModel = mainWindowViewModel;

        Jobs = new ObservableCollection<RunAllJobProgressItemViewModel>(
            jobsToRun.Select(job => new RunAllJobProgressItemViewModel(job)));

        _jobRowsByName = Jobs.ToDictionary(
            item => item.JobName,
            item => item,
            StringComparer.Ordinal);

        SetSelectedJobInternal(Jobs.FirstOrDefault());
        _summaryMessage = Jobs.Count == 0
            ? Text("GuiErrorNoJobToExecute")
            : SummaryPending;
    }

    /// <summary>
    /// Starts the run-all execution once the window is displayed.
    /// </summary>
    public async Task StartExecutionAsync()
    {
        if (_executionStarted)
            return;

        if (Jobs.Count == 0)
        {
            IsExecutionCompleted = true;
            return;
        }

        _executionStarted = true;
        IsRunning = true;
        IsExecutionCompleted = false;
        SummaryMessage = SummaryRunning;
        _selectionLockedByUser = false;

        RefreshProgressSnapshot();

        var (success, results, errorMessage) = await _mainWindowViewModel.ExecuteAllWithResultAsync();

        RefreshProgressSnapshot();
        IsRunning = false;
        IsExecutionCompleted = true;

        if (!success)
        {
            SummaryMessage = errorMessage;
            return;
        }

        var completed = results.Count(state => state.Status == BackupStatus.Completed);
        var errors = results.Count(state => state.Status == BackupStatus.Error || state.Status == BackupStatus.Cancelled);
        SummaryMessage = string.Format(Text("GuiStatusExecutionSummary"), results.Count, completed, errors);
        OnPropertyChanged(nameof(CanStartSelectedJob));
        OnPropertyChanged(nameof(CanPauseSelectedJob));
        OnPropertyChanged(nameof(CanCancelSelectedJob));
    }

    /// <summary>
    /// Pulls the latest snapshots from the job execution state file and updates row/detail display.
    /// </summary>
    public void RefreshProgressSnapshot()
    {
        _mainWindowViewModel.JobExecution.RefreshJobState();
        var states = _mainWindowViewModel.JobExecution.LatestProgressStates;
        if (states == null || states.Count == 0)
        {
            UpdateGlobalProgress();
            return;
        }

        RunAllJobProgressItemViewModel? mostRecentActiveJob = null;

        foreach (var state in states)
        {
            if (state.Job == null)
                continue;

            if (!_jobRowsByName.TryGetValue(state.Job.Name, out var targetRow))
                continue;

            targetRow.ApplyState(state, Text);

            if (state.Status == BackupStatus.Active || state.Status == BackupStatus.Paused)
            {
                mostRecentActiveJob = targetRow;
            }
        }

        if (SelectedJob == null)
        {
            SetSelectedJobInternal(mostRecentActiveJob ?? Jobs.FirstOrDefault());
        }
        else if (!_selectionLockedByUser && mostRecentActiveJob != null && !IsExecutionCompleted)
        {
            SetSelectedJobInternal(mostRecentActiveJob);
        }

        UpdateGlobalProgress();
        OnPropertyChanged(nameof(CanStartSelectedJob));
        OnPropertyChanged(nameof(CanPauseSelectedJob));
        OnPropertyChanged(nameof(CanCancelSelectedJob));
    }

    public void StartSelectedJob()
    {
        if (!CanStartSelectedJob || SelectedJob == null)
            return;

        _mainWindowViewModel.JobExecution.ResumeJob(SelectedJob.JobName);
        SummaryMessage = Text("GuiStatusJobResumed");
    }

    public void PauseSelectedJob()
    {
        if (!CanPauseSelectedJob || SelectedJob == null)
            return;

        _mainWindowViewModel.JobExecution.PauseJob(SelectedJob.JobName);
        SummaryMessage = Text("GuiStatusJobPaused");
    }

    public void CancelSelectedJob()
    {
        if (!CanCancelSelectedJob || SelectedJob == null)
            return;

        _mainWindowViewModel.JobExecution.StopJob(SelectedJob.JobName);
        SummaryMessage = Text("GuiStatusJobCancelled");
    }

    public void CancelAllRunningJobs()
    {
        if (!IsRunning)
            return;

        _mainWindowViewModel.JobExecution.StopAllJobs();
        SummaryMessage = SummaryCancelling;
    }

    private void SetSelectedJobInternal(RunAllJobProgressItemViewModel? job)
    {
        _suppressSelectionLock = true;
        SelectedJob = job;
        _suppressSelectionLock = false;
    }

    private void UpdateGlobalProgress()
    {
        if (Jobs.Count == 0)
        {
            GlobalProgress = 0;
            return;
        }

        GlobalProgress = Math.Clamp(Jobs.Average(item => item.Progress), 0, 100);
    }

    private string Text(string key) => _mainWindowViewModel.GetText(key);
}
