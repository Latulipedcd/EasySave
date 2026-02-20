using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Core.Enums;
using Core.Models;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// ViewModel principal de la fenêtre MainWindow
/// Orchestre les différents ViewModels spécialisés et gère les textes localisés
/// Implémente INotifyPropertyChanged pour le binding bidirectionnel avec l'UI
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    // ViewModel de la couche Application pour accéder aux services métier
    private readonly MainViewModel _appViewModel;

    // ViewModels spécialisés (depuis Application layer)
    public SettingsViewModel Settings => _appViewModel.Settings;
    public JobEditorViewModel JobEditor => _appViewModel.JobEditor;
    public JobListViewModel JobList => _appViewModel.JobList;
    public JobExecutionViewModel JobExecution => _appViewModel.JobExecution;

    private readonly System.Timers.Timer _stateRefreshTimer;
    private readonly SynchronizationContext? _uiContext;
    private string _catSpeech = string.Empty;

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
    public MainWindowViewModel(MainViewModel appViewModel)
    {
        _appViewModel = appViewModel;

        // Événements inter-ViewModels
        Settings.LanguageChanged += (s, e) => RefreshLocalizedTexts();
        JobList.SelectionChanged += (s, job) =>
        {
            JobExecution.MonitoredJob = job;
            SetDefaultCatMessage();
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

        SetDefaultCatMessage();

        // Initialisation du timer pour rafraîchir l'état des jobs
        _uiContext = SynchronizationContext.Current;
        _stateRefreshTimer = new System.Timers.Timer(500);
        _stateRefreshTimer.Elapsed += OnRefreshTimerTick;
        _stateRefreshTimer.AutoReset = true;
        _stateRefreshTimer.Start();
    }

    /// <summary>
    /// Handler appelé périodiquement pour rafraîchir l'état du job en cours d'exécution
    /// </summary>
    private void OnRefreshTimerTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _uiContext?.Post(_ => _appViewModel.RefreshJobState(), null);
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
        SetDefaultCatMessage();
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

    private void SetCatMessage(string key, params object[] args)
    {
        CatSpeech = args.Length > 0 ? TextFormat(key, args) : Text(key);
    }

    private void SetCatRawMessage(string message)
    {
        CatSpeech = message;
    }

    private void SetDefaultCatMessage()
    {
        if (HasSelection)
            SetCatMessage("GuiCatMessageSelected", SelectedJobName);
        else
            SetCatMessage("GuiCatMessageNoSelection");
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
    public async Task CreateJobAsync()
    {
        SetCatMessage("GuiStatusCreatingJob");
        SetCatMessage("GuiCatMessageCreating");

        var name = JobEditor.JobName;
        var (success, message) = await _appViewModel.CreateJobAsync();

        SetCatRawMessage(message);

        if (!success)
        {
            // Si le message correspond à l'erreur de nom vide, affiche en rouge dans le chat
            if (message == Text("GuiErrorJobNameEmpty"))
            {
                SetCatRawMessage(message);
                // Optionnel: déclencher une animation spécifique (ex: cat error)
                // AnimationCat = "cat error.json";
            }
            else
            {
                SetCatMessage("GuiCatMessageActionFailed", message);
            }
            return;
        }

        SetCatMessage("GuiCatMessageCreated", name);

        // Rafraîchir la liste des jobs
        var jobs = await _appViewModel.RefreshJobsAsync();
        _appViewModel.ReplaceJobs(jobs);
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
            SetCatMessage("GuiStatusEditModeHint");
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
    public async Task UpdateSelectedJobAsync()
    {
        SetCatMessage("GuiCatMessageUpdating");

        var (success, message, canContinue) = await _appViewModel.UpdateJobAsync();

        if (!canContinue)
        {
            // Si le message correspond à l'erreur de nom vide, affiche en rouge dans le chat
            if (message == Text("GuiErrorJobNameEmpty"))
            {
                SetCatRawMessage(message);
                // Optionnel: déclencher une animation spécifique (ex: cat error)
                // AnimationCat = "cat error.json";
            }
            else
            {
                SetStatusOnUIThread(message);
            }
            return;
        }

        SetCatRawMessage(message);

        if (!success)
        {
            SetCatRawMessage(message);
            return;
        }

        SetCatMessage("GuiCatMessageUpdated", SelectedJobName);

        // Rafraîchir la liste des jobs
        var jobs = await _appViewModel.RefreshJobsAsync();
        _appViewModel.ReplaceJobs(jobs);
    }

    /// <summary>
    /// Supprime les jobs sélectionnés de la liste
    /// </summary>
    public async Task DeleteSelectedJobsAsync(IReadOnlyList<BackupJob> selectedJobs)
    {
        if (selectedJobs == null || selectedJobs.Count == 0)
        {
            SetStatusOnUIThread(Text("GuiErrorNoJobSelected"));
            SetDefaultCatMessage();
            return;
        }

        SetStatusOnUIThread(Text("GuiStatusDeleting"));
        SetCatMessage("GuiCatMessageDeleting");

        var (deletedCount, errors) = await _appViewModel.DeleteJobsAsync(selectedJobs);

        // Rafraîchir la liste des jobs
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

   

    /// <summary>
    /// Exécute tous les jobs de backup de la liste
    /// </summary>
    public async Task ExecuteAllAsync()
    {
        SetStatusOnUIThread(Text("GuiStatusExecuting"));
        SetCatMessage("GuiCatMessageExecuting");

        var (success, results, errorMessage) = await _appViewModel.ExecuteAllJobsAsync();

        if (!success && BackupJobs.Count == 0)
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
        var errors = results.Count(r => r.Status == BackupStatus.Error);
        SetStatusOnUIThread(TextFormat("GuiStatusExecutionSummary", results.Count, completed, errors));

        if (errors == 0)
            SetCatMessage("GuiCatMessageExecutedSuccess", completed);
        else
            SetCatMessage("GuiCatMessageExecutedWithErrors", errors);
    }

    /// <summary>
    /// Exécute uniquement les jobs sélectionnés dans la liste
    /// </summary>
    public async Task ExecuteSelectedAsync(IReadOnlyList<BackupJob> selectedJobs)
    {
        SetStatusOnUIThread(Text("GuiStatusExecuting"));
        SetCatMessage("GuiCatMessageExecuting");

        var (success, results, errorMessage) = await _appViewModel.ExecuteSelectedJobsAsync(selectedJobs);

        if (!success && (selectedJobs == null || selectedJobs.Count == 0))
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
        var errors = results.Count(r => r.Status == BackupStatus.Error);
        SetStatusOnUIThread(TextFormat("GuiStatusExecutionSummary", results.Count, completed, errors));

        if (errors == 0)
            SetCatMessage("GuiCatMessageExecutedSuccess", completed);
        else
            SetCatMessage("GuiCatMessageExecutedWithErrors", errors);
    }

  

    /// <summary>
    /// Définit le message de statut de manière thread-safe
    /// S'assure que la mise à jour se fait sur le thread UI
    /// </summary>
    private void SetStatusOnUIThread(string message)
    {
        if (_uiContext == null || SynchronizationContext.Current == _uiContext)
        {
            StatusMessage = message;
            return;
        }

        _uiContext.Post(_ => StatusMessage = message, null);
    }

    /// <summary>
    /// Méthode publique pour définir le message de statut
    /// Utilisée par les fenêtres enfants (JobEditorWindow)
    /// </summary>
    /// <param name="message">Message à afficher</param>
    public void SetStatus(string message)
    {
        SetStatusOnUIThread(message);
        SetCatRawMessage(message);
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
    
