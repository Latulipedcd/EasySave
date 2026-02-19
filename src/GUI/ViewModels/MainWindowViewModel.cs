using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Threading;
using Core.Enums;
using Core.Models;
using EasySave.Application.ViewModels;

namespace GUI.ViewModels;

/// <summary>
/// ViewModel principal de la fenêtre MainWindow
/// Orchestre les différents ViewModels spécialisés et gère les textes localisés
/// Implémente INotifyPropertyChanged pour le binding bidirectionnel avec l'UI
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    // Chat assistant notification types
    private enum CatNotification
    {
        NoSelection,
        SelectedReady,
        SelectedRunning,
        Creating,
        Created,
        Updating,
        Updated,
        Deleting,
        Deleted,
        DeleteWithErrors,
        ExecutingAll,
        ExecutingSelected,
        ExecutedSuccess,
        ExecutedWithErrors,
        ActionFailed,
        NoJobToRun,
        EditModeHint
    }

    // ViewModel de la couche Application pour accéder aux services métier
    private readonly MainViewModel _appViewModel;

    // ViewModels spécialisés (depuis Application layer)
    public SettingsViewModel Settings => _appViewModel.Settings;
    public JobEditorViewModel JobEditor => _appViewModel.JobEditor;
    public JobListViewModel JobList => _appViewModel.JobList;
    public JobExecutionViewModel JobExecution => _appViewModel.JobExecution;

    private readonly DispatcherTimer _stateRefreshTimer;
    private string _catSpeech = string.Empty;
    private DateTime _catPriorityUntilUtc = DateTime.MinValue;
    private bool _pendingSelectionNotification;
    private const int CatPriorityHoldSeconds = 4;
    private static readonly string[] DirectValidationErrorKeys =
    {
        "GuiErrorJobNameEmpty"
    };

    /// <summary>
    /// Récupère un texte localisé depuis les fichiers de ressources
    /// </summary>
    private string Text(string key) => _appViewModel.GetText(key);
    
    /// <summary>
    /// Récupère un texte localisé formaté avec des paramètres
    /// </summary>
    private string TextFormat(string key, params object[] args) => string.Format(Text(key), args);

  

   
    // Textes de la fenêtre et de l'en-tête
    public string WindowTitle => Text("GuiWindowTitle");
    public string JobsHeaderTitle => Text("GuiJobsHeaderTitle");
    public string ExecuteSelectedLabel => Text("GuiExecuteSelected");
    public string ExecuteAllLabel => Text("GuiExecuteAll");
    public string DeleteSelectedLabel => Text("GuiDeleteSelected");
    public string LanguageLabel => Text("GuiLanguageLabel");
    public string LogFormatLabel => Text("GuiLogFormatLabel");
    public string LogFormatJsonLabel => Text("GuiLogFormatJson");
    public string LogFormatXmlLabel => Text("GuiLogFormatXml");
    public string BusinessSoftwareLabel => Text("GuiBusinessSoftwareLabel");
    public string CryptoExtensionsLabel => Text("GuiCryptoExtensionsLabel");
    public string SettingsButtonLabel => Text("GuiSettingsButton");
    public string SettingsMenuTitle => Text("GuiSettingsMenuTitle");

    // Textes des en-têtes de colonnes
    public string HeaderName => Text("GuiHeaderName");
    public string HeaderSource => Text("GuiHeaderSource");
    public string HeaderDestination => Text("GuiHeaderDestination");
    public string HeaderType => Text("GuiHeaderType");

    // Textes du formulaire de création/modification
    public string SectionCreateEdit => Text("GuiSectionCreateEdit");
    public string LabelName => Text("GuiLabelName");
    public string LabelSourceFolder => Text("GuiLabelSourceFolder");
    public string LabelDestinationFolder => Text("GuiLabelDestinationFolder");
    public string LabelType => Text("GuiLabelType");
    public string BrowseLabel => Text("GuiBrowse");
    public string CreateLabel => Text("GuiCreate");
    public string UpdateLabel => Text("GuiUpdate");
    public string SaveLabel => Text("GuiSave");
    public string CancelLabel => Text("GuiCancel");
    public string NewJobButtonLabel => Text("GuiNewJobButton");
    public string EditJobButtonLabel => Text("GuiEditJobButton");

    // Textes des types de backup
    public string JobTypeFullLabel => Text("GuiJobTypeFull");
    public string JobTypeDifferentialLabel => Text("GuiJobTypeDifferential");

    // Textes du panneau de détails
    public string JobDetailsTitle => Text("GuiJobDetailsTitle");
    public string JobDetailsNoSelection => Text("GuiJobDetailsNoSelection");
    public string JobDetailsStatus => Text("GuiJobDetailsStatus");
    public string JobDetailsProgress => Text("GuiJobDetailsProgress");
    public string JobDetailsTotalFiles => Text("GuiJobDetailsTotalFiles");
    public string JobDetailsFilesRemaining => Text("GuiJobDetailsFilesRemaining");
    public string JobDetailsTotalSize => Text("GuiJobDetailsTotalSize");
    public string JobDetailsSizeRemaining => Text("GuiJobDetailsSizeRemaining");
    public string JobDetailsCurrentFile => Text("GuiJobDetailsCurrentFile");
    public string JobDetailsLastUpdate => Text("GuiJobDetailsLastUpdate");
    public string CatWidgetTitle => Text("GuiCatWidgetTitle");

    /// <summary>
    /// Titre de la fenêtre d'édition (dynamique selon le mode création/modification)
    /// </summary>
    public string EditorWindowTitle => IsEditing ? Text("GuiEditorWindowTitleEdit") : Text("GuiEditorWindowTitleNew");

    /// <summary>
    /// Méthode publique pour récupérer un texte localisé (utilisée par MainWindow)
    /// </summary>
    public string GetText(string key) => Text(key);

    
    // Délégation aux ViewModels spécialisés
    public ObservableCollection<BackupJob> BackupJobs => JobList.BackupJobs;
    public ObservableCollection<BackupJobDisplayItem> DisplayJobs => JobList.DisplayJobs;
    public ObservableCollection<string> SupportedLanguages => Settings.SupportedLanguages;
    public ObservableCollection<SettingItemViewModel> SettingsItems => Settings.SettingsItems;
    
    public int SelectedLogFormatIndex
    {
        get => Settings.SelectedLogFormatIndex;
        set => Settings.SelectedLogFormatIndex = value;
    }

    public string SelectedLanguageCode
    {
        get => Settings.SelectedLanguageCode;
        set => Settings.SelectedLanguageCode = value;
    }

    public string BusinessSoftware
    {
        get => Settings.BusinessSoftware;
        set => Settings.BusinessSoftware = value;
    }

    public string CryptoExtensions
    {
        get => Settings.CryptoExtensions;
        set => Settings.CryptoExtensions = value;
    }


    /// <summary>
    /// Message de statut affiché dans la barre de status
    /// Ex: "Création du job...", "Job créé avec succès", etc.
    /// </summary>
    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (value == _statusMessage) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Constructeur du ViewModel
    /// Initialise les ViewModels spécialisés et charge les données sauvegardées
    /// </summary>
    public MainWindowViewModel()
    {
        // Initialisation du ViewModel de la couche Application
        _appViewModel = new MainViewModel();

        // Événements inter-ViewModels
        Settings.LanguageChanged += (s, e) => RefreshLocalizedTexts();
        JobList.SelectionChanged += (s, job) =>
        {
            JobExecution.MonitoredJob = job;
            UpdateSelectionNotification();
            OnPropertyChanged(nameof(SelectedJob));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectedJobName));
            OnPropertyChanged(nameof(SelectedJobSource));
            OnPropertyChanged(nameof(SelectedJobTarget));
            OnPropertyChanged(nameof(SelectedJobType));
        };
        JobExecution.StateChanged += (s, state) =>
        {
            OnPropertyChanged(nameof(SelectedJobState));
            OnPropertyChanged(nameof(IsJobRunning));
            OnPropertyChanged(nameof(ShowExecutionDetails));
            OnPropertyChanged(nameof(IsJobCompleted));
            OnPropertyChanged(nameof(JobStatusText));
            OnPropertyChanged(nameof(JobStatusColor));
            OnPropertyChanged(nameof(JobProgress));
            OnPropertyChanged(nameof(JobTotalFiles));
            OnPropertyChanged(nameof(JobFilesRemaining));
            OnPropertyChanged(nameof(JobTotalSize));
            OnPropertyChanged(nameof(JobSizeRemaining));
            OnPropertyChanged(nameof(JobCurrentFile));
            OnPropertyChanged(nameof(JobLastUpdate));

            // Keep assistant message aligned with runtime status when a selected job is running.
            if (HasSelection && state?.Status == BackupStatus.Active)
            {
                UpdateSelectionNotification();
            }
        };
        JobEditor.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(JobEditor.EditingJobId) || e.PropertyName == nameof(JobEditor.IsEditing))
            {
                OnPropertyChanged(nameof(EditingJobId));
                OnPropertyChanged(nameof(IsEditing));
                OnPropertyChanged(nameof(EditorWindowTitle));
            }
            else if (e.PropertyName == nameof(JobEditor.JobName))
                OnPropertyChanged(nameof(NewJobName));
            else if (e.PropertyName == nameof(JobEditor.SourceDirectory))
                OnPropertyChanged(nameof(NewJobSourceDirectory));
            else if (e.PropertyName == nameof(JobEditor.TargetDirectory))
                OnPropertyChanged(nameof(NewJobTargetDirectory));
            else if (e.PropertyName == nameof(JobEditor.BackupTypeIndex))
                OnPropertyChanged(nameof(NewJobTypeIndex));
        };

        // Initialisation des textes localisés et construction du menu settings
        RefreshLocalizedTexts();
        OnPropertyChanged(nameof(SelectedLanguageCode));
        OnPropertyChanged(nameof(SelectedLogFormatIndex));

        UpdateSelectionNotification();

        // Initialisation du timer pour rafraîchir l'état des jobs
        _stateRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _stateRefreshTimer.Tick += OnRefreshTimerTick;
        _stateRefreshTimer.Start();
    }

    /// <summary>
    /// Handler appelé périodiquement pour rafraîchir l'état du job en cours d'exécution
    /// </summary>
    private void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        _appViewModel.RefreshJobState();

        if (_pendingSelectionNotification && !IsCatMessagePinned)
        {
            UpdateSelectionNotification(force: true);
        }
    }

    /// <summary>
    /// Rafraîchit tous les textes localisés de l'interface
    /// Appelée après un changement de langue pour mettre à jour l'UI
    /// </summary>
    private void RefreshLocalizedTexts()
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(JobsHeaderTitle));
        OnPropertyChanged(nameof(ExecuteSelectedLabel));
        OnPropertyChanged(nameof(ExecuteAllLabel));
        OnPropertyChanged(nameof(DeleteSelectedLabel));
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(LogFormatLabel));
        OnPropertyChanged(nameof(LogFormatJsonLabel));
        OnPropertyChanged(nameof(LogFormatXmlLabel));
        OnPropertyChanged(nameof(SettingsButtonLabel));
        OnPropertyChanged(nameof(SettingsMenuTitle));
        OnPropertyChanged(nameof(HeaderName));
        OnPropertyChanged(nameof(HeaderSource));
        OnPropertyChanged(nameof(HeaderDestination));
        OnPropertyChanged(nameof(HeaderType));
        OnPropertyChanged(nameof(SectionCreateEdit));
        OnPropertyChanged(nameof(LabelName));
        OnPropertyChanged(nameof(LabelSourceFolder));
        OnPropertyChanged(nameof(LabelDestinationFolder));
        OnPropertyChanged(nameof(LabelType));
        OnPropertyChanged(nameof(BrowseLabel));
        OnPropertyChanged(nameof(CreateLabel));
        OnPropertyChanged(nameof(UpdateLabel));
        OnPropertyChanged(nameof(SaveLabel));
        OnPropertyChanged(nameof(CancelLabel));
        OnPropertyChanged(nameof(NewJobButtonLabel));
        OnPropertyChanged(nameof(EditJobButtonLabel));
        OnPropertyChanged(nameof(JobTypeFullLabel));
        OnPropertyChanged(nameof(JobTypeDifferentialLabel));
        OnPropertyChanged(nameof(JobDetailsTitle));
        OnPropertyChanged(nameof(JobDetailsNoSelection));
        OnPropertyChanged(nameof(JobDetailsStatus));
        OnPropertyChanged(nameof(JobDetailsProgress));
        OnPropertyChanged(nameof(JobDetailsTotalFiles));
        OnPropertyChanged(nameof(JobDetailsFilesRemaining));
        OnPropertyChanged(nameof(JobDetailsTotalSize));
        OnPropertyChanged(nameof(JobDetailsSizeRemaining));
        OnPropertyChanged(nameof(JobDetailsCurrentFile));
        OnPropertyChanged(nameof(JobDetailsLastUpdate));
        OnPropertyChanged(nameof(CatWidgetTitle));
        UpdateSelectionNotification();
        OnPropertyChanged(nameof(EditorWindowTitle));
        OnPropertyChanged(nameof(JobStatusText));
        
        // Build or refresh settings menu items
        if (Settings.SettingsItems.Count == 0)
        {
            Settings.BuildSettingsMenuItems(
                LanguageLabel, LogFormatLabel, LogFormatJsonLabel, LogFormatXmlLabel,
                BusinessSoftwareLabel, CryptoExtensionsLabel);
        }
        else
        {
            Settings.RefreshSettingsMenuItems(
                LanguageLabel, LogFormatLabel, LogFormatJsonLabel, LogFormatXmlLabel,
                BusinessSoftwareLabel, CryptoExtensionsLabel);
        }
    }

    // Délégation pour JobEditor
    public string NewJobName
    {
        get => JobEditor.JobName;
        set => JobEditor.JobName = value;
    }

    public string NewJobSourceDirectory
    {
        get => JobEditor.SourceDirectory;
        set => JobEditor.SourceDirectory = value;
    }

    public string NewJobTargetDirectory
    {
        get => JobEditor.TargetDirectory;
        set => JobEditor.TargetDirectory = value;
    }

    public int NewJobTypeIndex
    {
        get => JobEditor.BackupTypeIndex;
        set => JobEditor.BackupTypeIndex = value;
    }

    public int? EditingJobId => JobEditor.EditingJobId;
    public bool IsEditing => JobEditor.IsEditing;

    // Délégation pour JobList
    public BackupJob? SelectedJob
    {
        get => JobList.SelectedJob;
        set => JobList.SelectedJob = value;
    }

    public bool HasSelection => JobList.HasSelection;
    public string SelectedJobName => JobList.SelectedJobName;
    public string SelectedJobSource => JobList.SelectedJobSource;
    public string SelectedJobTarget => JobList.SelectedJobTarget;
    public string SelectedJobType => JobList.SelectedJobType;

    // Délégation pour JobExecution
    public BackupState? SelectedJobState => JobExecution.JobState;
    public bool IsJobRunning => JobExecution.IsJobRunning;
    public bool ShowExecutionDetails => JobExecution.ShowExecutionDetails;
    public bool IsJobCompleted => JobExecution.IsJobCompleted;

    public string JobStatusText => SelectedJobState?.Status switch
    {
        BackupStatus.Inactive => Text("GuiStatusInactive"),
        BackupStatus.Active => Text("GuiStatusActive"),
        BackupStatus.Completed => Text("GuiStatusCompleted"),
        BackupStatus.Error => Text("GuiStatusError"),
        _ => Text("GuiStatusInactive")
    };

    public string JobStatusColor => SelectedJobState?.Status switch
    {
        BackupStatus.Active => "#3498db",
        BackupStatus.Completed => "#27ae60",
        BackupStatus.Error => "#e74c3c",
        _ => "#95a5a6"
    };

    public double JobProgress => JobExecution.JobProgress;
    public string JobTotalFiles => JobExecution.JobTotalFiles;
    public string JobFilesRemaining => JobExecution.JobFilesRemaining;
    public string JobTotalSize => JobExecution.JobTotalSize;
    public string JobSizeRemaining => JobExecution.JobSizeRemaining;
    public string JobCurrentFile => JobExecution.JobCurrentFile;
    public string JobLastUpdate => JobExecution.JobLastUpdate;
    
    // Chat assistant notification state and helpers
    public string CatSpeech
    {
        get => _catSpeech;
        private set
        {
            if (value == _catSpeech) return;
            _catSpeech = value;
            OnPropertyChanged();
        }
    }

    private bool IsCatMessagePinned => DateTime.UtcNow < _catPriorityUntilUtc;

    private static bool IsContextNotification(CatNotification notification)
    {
        return notification is CatNotification.NoSelection
            or CatNotification.SelectedReady
            or CatNotification.SelectedRunning;
    }

    private void PublishCatSpeech(string message, bool pinMessage)
    {
        CatSpeech = message;

        if (pinMessage)
        {
            _catPriorityUntilUtc = DateTime.UtcNow.AddSeconds(CatPriorityHoldSeconds);
            _pendingSelectionNotification = false;
        }
    }

    private void NotifyCat(CatNotification notification, params object[] args)
    {
        var key = notification switch
        {
            CatNotification.NoSelection => "GuiCatMessageNoSelection",
            CatNotification.SelectedReady => "GuiCatMessageSelected",
            CatNotification.SelectedRunning => "GuiCatMessageSelectedRunning",
            CatNotification.Creating => "GuiCatMessageCreating",
            CatNotification.Created => "GuiCatMessageCreated",
            CatNotification.Updating => "GuiCatMessageUpdating",
            CatNotification.Updated => "GuiCatMessageUpdated",
            CatNotification.Deleting => "GuiCatMessageDeletingCount",
            CatNotification.Deleted => "GuiCatMessageDeleted",
            CatNotification.DeleteWithErrors => "GuiCatMessageDeleteWithErrors",
            CatNotification.ExecutingAll => "GuiCatMessageExecutingAll",
            CatNotification.ExecutingSelected => "GuiCatMessageExecutingSelected",
            CatNotification.ExecutedSuccess => "GuiCatMessageExecutedSuccess",
            CatNotification.ExecutedWithErrors => "GuiCatMessageExecutedWithErrors",
            CatNotification.ActionFailed => "GuiCatMessageActionFailed",
            CatNotification.NoJobToRun => "GuiCatMessageNoJobToRun",
            CatNotification.EditModeHint => "GuiStatusEditModeHint",
            _ => "GuiCatMessageNoSelection"
        };

        var message = args.Length > 0 ? TextFormat(key, args) : Text(key);
        PublishCatSpeech(message, pinMessage: !IsContextNotification(notification));
    }

    private void NotifyCatRaw(string message, bool pinMessage = true)
    {
        PublishCatSpeech(message, pinMessage);
    }

    /// <summary>
    /// Méthode publique pour définir le message de statut
    /// Utilisée par les fenêtres enfants (JobEditorWindow)
    /// </summary>
    /// <param name="message">Message à afficher</param>
    public void SetStatus(string message)
    {
        NotifyCatRaw(message);
    }

    private bool IsDirectValidationError(string message)
    {
        return DirectValidationErrorKeys.Any(key => string.Equals(message, Text(key), StringComparison.Ordinal));
    }

    private bool TryNotifyDirectValidationError(string message)
    {
        if (!IsDirectValidationError(message))
            return false;

        NotifyCatRaw(message);
        return true;
    }

    private void UpdateSelectionNotification(bool force = false)
    {
        if (!force && IsCatMessagePinned)
        {
            _pendingSelectionNotification = true;
            return;
        }

        _pendingSelectionNotification = false;

        if (!HasSelection)
        {
            NotifyCat(CatNotification.NoSelection);
            return;
        }

        if (SelectedJobState?.Status == BackupStatus.Active)
        {
            NotifyCat(CatNotification.SelectedRunning, SelectedJobName);
            return;
        }

        NotifyCat(CatNotification.SelectedReady, SelectedJobName);
    }

    /// <summary>
    /// Efface l'état d'exécution affiché
    /// </summary>
    public void ClearJobState()
    {
        _appViewModel.ClearJobState();
    }

    /// <summary>
    /// Crée un nouveau job de sauvegarde de manière asynchrone
    /// </summary>
    public async Task<bool> CreateJobAsync()
    {
        NotifyCat(CatNotification.Creating);

        var name = JobEditor.JobName;
        var (success, message) = await _appViewModel.CreateJobAsync();

        if (!success)
        {
            if (!TryNotifyDirectValidationError(message))
                NotifyCat(CatNotification.ActionFailed, message);
            return false;
        }

        NotifyCat(CatNotification.Created, name);

        // Rafraîchir la liste des jobs
        var jobs = await _appViewModel.RefreshJobsAsync();
        await Dispatcher.UIThread.InvokeAsync(() => _appViewModel.ReplaceJobs(jobs));
        return true;
    }

    /// <summary>
    /// Charge les données d'un job existant dans le formulaire pour modification
    /// </summary>
    public void LoadSelectionForEdit(BackupJob job)
    {
        _appViewModel.LoadJobForEdit(job);
        // Message d'édition relocalisé dans le chat
        if (job != null && JobList.GetJobId(job) > 0)
        {
            NotifyCat(CatNotification.EditModeHint);
        }
    }

    /// <summary>
    /// Réinitialise le mode d'édition en annulant les modifications
    /// </summary>
    public void ClearSelectionForEdit()
    {
        _appViewModel.ClearJobEditor();
    }

    /// <summary>
    /// Met à jour le job en cours de modification avec les nouvelles données du formulaire
    /// </summary>
    public async Task<bool> UpdateSelectedJobAsync()
    {
        NotifyCat(CatNotification.Updating);

        var (success, message, canContinue) = await _appViewModel.UpdateJobAsync();

        if (!canContinue)
        {
            if (!TryNotifyDirectValidationError(message))
                NotifyCatRaw(message);
            return false;
        }

        if (!success)
        {
            NotifyCat(CatNotification.ActionFailed, message);
            return false;
        }

        NotifyCat(CatNotification.Updated, SelectedJobName);

        // Rafraîchir la liste des jobs
        var jobs = await _appViewModel.RefreshJobsAsync();
        await Dispatcher.UIThread.InvokeAsync(() => _appViewModel.ReplaceJobs(jobs));
        return true;
    }

    /// <summary>
    /// Supprime les jobs sélectionnés de la liste
    /// </summary>
    public async Task DeleteSelectedJobsAsync(IReadOnlyList<BackupJob> selectedJobs)
    {
        if (selectedJobs == null || selectedJobs.Count == 0)
        {
            SetStatusOnUIThread(Text("GuiErrorNoJobSelected"));
            NotifyCatRaw(Text("GuiErrorNoJobSelected"));
            return;
        }

        SetStatusOnUIThread(Text("GuiStatusDeleting"));
        NotifyCat(CatNotification.Deleting, selectedJobs.Count);

        var (deletedCount, errors) = await _appViewModel.DeleteJobsAsync(selectedJobs);

        // Rafraîchir la liste des jobs
        var jobs = await _appViewModel.RefreshJobsAsync();
        await Dispatcher.UIThread.InvokeAsync(() => _appViewModel.ReplaceJobs(jobs));

        if (errors.Count == 0)
        {
            SetStatusOnUIThread(TextFormat("GuiStatusDeletedCount", deletedCount));
            NotifyCat(CatNotification.Deleted, deletedCount);
            return;
        }

        SetStatusOnUIThread(TextFormat("GuiStatusDeletedWithErrors", deletedCount, errors.Count, errors[0]));
        NotifyCat(CatNotification.DeleteWithErrors, errors.Count);
    }

   

    /// <summary>
    /// Exécute tous les jobs de backup de la liste
    /// </summary>
    public async Task ExecuteAllAsync()
    {
        SetStatusOnUIThread(Text("GuiStatusExecuting"));
        NotifyCat(CatNotification.ExecutingAll, BackupJobs.Count);

        var (success, results, errorMessage) = await _appViewModel.ExecuteAllJobsAsync();

        if (!success && BackupJobs.Count == 0)
        {
            StatusMessage = errorMessage;
            NotifyCat(CatNotification.NoJobToRun);
            return;
        }

        if (!success)
        {
            SetStatusOnUIThread(errorMessage);
            NotifyCat(CatNotification.ActionFailed, errorMessage);
            return;
        }

        var completed = results.Count(r => r.Status == BackupStatus.Completed);
        var errors = results.Count(r => r.Status == BackupStatus.Error);
        SetStatusOnUIThread(TextFormat("GuiStatusExecutionSummary", results.Count, completed, errors));

        if (errors == 0)
            NotifyCat(CatNotification.ExecutedSuccess, completed);
        else
            NotifyCat(CatNotification.ExecutedWithErrors, errors);
    }

    /// <summary>
    /// Exécute uniquement les jobs sélectionnés dans la liste
    /// </summary>
    public async Task ExecuteSelectedAsync(IReadOnlyList<BackupJob> selectedJobs)
    {
        if (selectedJobs == null || selectedJobs.Count == 0)
        {
            SetStatusOnUIThread(Text("GuiErrorNoJobSelected"));
            NotifyCatRaw(Text("GuiErrorNoJobSelected"));
            return;
        }

        SetStatusOnUIThread(Text("GuiStatusExecuting"));
        NotifyCat(CatNotification.ExecutingSelected, selectedJobs.Count);

        var (success, results, errorMessage) = await _appViewModel.ExecuteSelectedJobsAsync(selectedJobs);

        if (!success)
        {
            SetStatusOnUIThread(errorMessage);
            NotifyCat(CatNotification.ActionFailed, errorMessage);
            return;
        }

        var completed = results.Count(r => r.Status == BackupStatus.Completed);
        var errors = results.Count(r => r.Status == BackupStatus.Error);
        SetStatusOnUIThread(TextFormat("GuiStatusExecutionSummary", results.Count, completed, errors));

        if (errors == 0)
            NotifyCat(CatNotification.ExecutedSuccess, completed);
        else
            NotifyCat(CatNotification.ExecutedWithErrors, errors);
    }

  

    /// <summary>
    /// Définit le message de statut de manière thread-safe
    /// S'assure que la mise à jour se fait sur le thread UI
    /// </summary>
    private void SetStatusOnUIThread(string message)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            StatusMessage = message;
            return;
        }

        Dispatcher.UIThread.Post(() => StatusMessage = message);
    }

    /// <summary>
    /// Événement déclenché lorsqu'une propriété change
    /// Utilisé par le système de binding d'Avalonia pour mettre à jour l'UI
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    
    /// <summary>
    /// Notifie l'UI qu'une propriété a changé
    /// Le nom de la propriété est automatiquement déterminé par CallerMemberName
    /// </summary>
    /// <param name="propertyName">Nom de la propriété (automatique)</param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

   
}
    
