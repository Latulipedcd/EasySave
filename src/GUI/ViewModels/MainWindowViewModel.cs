using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Threading;
using Core.Enums;
using Core.Models;
using EasySave.Application.ViewModels;
using Log.Enums;

namespace GUI.ViewModels;

/// <summary>
/// ViewModel principal de la fenêtre MainWindow
/// Gère toute la logique métier et les données de l'interface graphique
/// Implémente INotifyPropertyChanged pour le binding bidirectionnel avec l'UI
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    // ViewModel de la couche Application pour accéder aux services métier
    private readonly MainViewModel _appViewModel;
    private SettingItemViewModel? _languageSetting;
    private SettingItemViewModel? _logFormatSetting;
    private SettingItemViewModel? _businessSoftwareSetting;
    private SettingItemViewModel? _cryptoExtensionsSetting;

    // Timer pour rafraîchir périodiquement l'état du job en cours d'exécution
    private readonly DispatcherTimer _stateRefreshTimer;
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

   

    /// <summary>
    /// Collection observable des jobs de sauvegarde
    /// Liée à la ListBox dans l'interface
    /// </summary>
    public ObservableCollection<BackupJob> BackupJobs { get; }
    
    /// <summary>
    /// Collection des langues supportées (en, fr, etc.)
    /// </summary>
    public ObservableCollection<string> SupportedLanguages { get; }

    /// <summary>
    /// Liste modulaire des options affichées dans le menu Settings.
    /// Chaque entrée contient son libellé, ses options et sa logique de persistance.
    /// </summary>
    public ObservableCollection<SettingItemViewModel> SettingsItems { get; }

   

    /// <summary>
    /// Index du format de logs sélectionné (0=Json, 1=Xml)
    /// Sauvegardé dans userconfig.json
    /// </summary>
    private int _selectedLogFormatIndex;
    public int SelectedLogFormatIndex
    {
        get => _selectedLogFormatIndex;
        set
        {
            if (value == _selectedLogFormatIndex) return;
            if (value != 0 && value != 1) return; // Validation : uniquement 0 ou 1

            _selectedLogFormatIndex = value;
            OnPropertyChanged();

            // Sauvegarde du format dans le fichier de configuration
            _appViewModel.ChangeLogFormat(value == 1 ? "Xml" : "Json");
            _logFormatSetting?.SetSelectedValue(value == 1 ? "Xml" : "Json");
        }
    }

    /// <summary>
    /// Code de la langue sélectionnée (en, fr, etc.)
    /// Sauvegardé dans userconfig.json et charge les ressources correspondantes
    /// </summary>
    private string _selectedLanguageCode = "en";
    public string SelectedLanguageCode
    {
        get => _selectedLanguageCode;
        set
        {
            if (value == _selectedLanguageCode) return;

            var previous = _selectedLanguageCode;
            _selectedLanguageCode = value;
            OnPropertyChanged();

            if (string.IsNullOrWhiteSpace(value))
                return;

            // Tentative de changement de langue
            var ok = _appViewModel.ChangeLanguage(value);
            if (!ok)
            {
                // Rollback si échec
                _selectedLanguageCode = previous;
                OnPropertyChanged(nameof(SelectedLanguageCode));
                _languageSetting?.SetSelectedValue(_selectedLanguageCode);
                return;
            }

            _languageSetting?.SetSelectedValue(_selectedLanguageCode);
            // Rafraîchissement de tous les textes de l'interface
            RefreshLocalizedTexts();
        }
    }

    /// <summary>
    /// Nom du logiciel métier à bloquer pendant les sauvegardes
    /// Sauvegardé dans userconfig.json
    /// </summary>
    private string _businessSoftware = string.Empty;
    public string BusinessSoftware
    {
        get => _businessSoftware;
        set
        {
            if (value == _businessSoftware) return;

            _businessSoftware = value ?? string.Empty;
            OnPropertyChanged();

            // Sauvegarde dans le fichier de configuration
            _appViewModel.ChangeBusinessSoftware(_businessSoftware);
            _businessSoftwareSetting?.SetTextValue(_businessSoftware);
        }
    }

    /// <summary>
    /// Extensions de fichiers à chiffrer (séparées par des virgules)
    /// Sauvegardé dans userconfig.json
    /// </summary>
    private string _cryptoExtensions = string.Empty;
    public string CryptoExtensions
    {
        get => _cryptoExtensions;
        set
        {
            if (value == _cryptoExtensions) return;

            _cryptoExtensions = value ?? string.Empty;
            OnPropertyChanged();

            // Parse comma-separated extensions and save to config
            var extensionsList = _cryptoExtensions
                .Split(',')
                .Select(ext => ext.Trim())
                .Where(ext => !string.IsNullOrWhiteSpace(ext))
                .ToList();

            _appViewModel.ChangeCryptoSoftExtensions(extensionsList);
            _cryptoExtensionsSetting?.SetTextValue(_cryptoExtensions);
        }
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
    /// Initialise toutes les propriétés et charge les données sauvegardées
    /// </summary>
    public MainWindowViewModel()
    {
        // Initialisation du ViewModel de la couche Application
        _appViewModel = new MainViewModel();
        
        // Chargement de la langue sauvegardée dans userconfig.json
        _appViewModel.TryLoadSavedLanguage();

        // Initialisation de la liste des langues supportées
        SupportedLanguages = new ObservableCollection<string>(_appViewModel.GetSupportedLanguages());
        _selectedLanguageCode = _appViewModel.CurrentLanguageCode;

        // Chargement du format de logs sauvegardé
        _selectedLogFormatIndex = _appViewModel.GetSavedLogFormat() == LogFormat.Xml ? 1 : 0;
        
        // Chargement des valeurs sauvegardées pour le logiciel métier et les extensions
        _businessSoftware = _appViewModel.GetSavedBusinessSoftware() ?? string.Empty;
        _cryptoExtensions = string.Join(", ", _appViewModel.GetCryptoSoftExtensions());
        
        // Construction des options du menu settings de manière modulaire
        SettingsItems = new ObservableCollection<SettingItemViewModel>();
        BuildSettingsMenuItems();

        // Chargement de la liste des jobs depuis le stockage
        BackupJobs = new ObservableCollection<BackupJob>(_appViewModel.GetBackupJobs());
        _newJobTypeIndex = 0;

        // Initialisation des textes localisés
        RefreshLocalizedTexts();
        OnPropertyChanged(nameof(SelectedLanguageCode));
        OnPropertyChanged(nameof(SelectedLogFormatIndex));

        // Initialisation du timer pour rafraîchir l'état du job sélectionné toutes les 500ms
        _stateRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _stateRefreshTimer.Tick += (sender, e) =>
        {
            // Rafraîchir l'état si un job est sélectionné
            if (SelectedJob != null)
            {
                RefreshSelectedJobState();
            }
        };
        _stateRefreshTimer.Start();

        SetDefaultCatMessage();

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
        RefreshSettingsMenuItems();
    }

    private void BuildSettingsMenuItems()
    {
        SettingsItems.Clear();

        _languageSetting = new SettingItemViewModel(
            LanguageLabel,
            SupportedLanguages.Select(code => new SettingOptionViewModel(code, code.ToUpperInvariant())),
            SelectedLanguageCode,
            value => SelectedLanguageCode = value);

        _logFormatSetting = new SettingItemViewModel(
            LogFormatLabel,
            GetLogFormatOptions(),
            SelectedLogFormatIndex == 1 ? "Xml" : "Json",
            value => SelectedLogFormatIndex =
                string.Equals(value, "Xml", StringComparison.OrdinalIgnoreCase) ? 1 : 0);

        _businessSoftwareSetting = new SettingItemViewModel(
            BusinessSoftwareLabel,
            _businessSoftware,
            value => BusinessSoftware = value);

        _cryptoExtensionsSetting = new SettingItemViewModel(
            CryptoExtensionsLabel,
            _cryptoExtensions,
            value => CryptoExtensions = value);

        SettingsItems.Add(_languageSetting);
        SettingsItems.Add(_logFormatSetting);
        SettingsItems.Add(_businessSoftwareSetting);
        SettingsItems.Add(_cryptoExtensionsSetting);
    }

    private IEnumerable<SettingOptionViewModel> GetLogFormatOptions()
    {
        yield return new SettingOptionViewModel("Json", LogFormatJsonLabel);
        yield return new SettingOptionViewModel("Xml", LogFormatXmlLabel);
    }

    private void RefreshSettingsMenuItems()
    {
        if (_languageSetting != null)
        {
            _languageSetting.UpdateLabel(LanguageLabel);
            _languageSetting.SetSelectedValue(SelectedLanguageCode);
        }

        if (_logFormatSetting != null)
        {
            _logFormatSetting.UpdateLabel(LogFormatLabel);
            _logFormatSetting.ReplaceOptions(
                GetLogFormatOptions(),
                SelectedLogFormatIndex == 1 ? "Xml" : "Json");
        }

        if (_businessSoftwareSetting != null)
        {
            _businessSoftwareSetting.UpdateLabel(BusinessSoftwareLabel);
            _businessSoftwareSetting.SetTextValue(_businessSoftware);
        }

        if (_cryptoExtensionsSetting != null)
        {
            _cryptoExtensionsSetting.UpdateLabel(CryptoExtensionsLabel);
            _cryptoExtensionsSetting.SetTextValue(_cryptoExtensions);
        }

        // Force le refresh du contenu si le flyout est déjà ouvert.
        OnPropertyChanged(nameof(SettingsItems));
    }


    /// <summary>
    /// Nom du nouveau job ou du job en cours de modification
    /// </summary>
    private string _newJobName = string.Empty;
    public string NewJobName
    {
        get => _newJobName;
        set
        {
            if (value == _newJobName) return;
            _newJobName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Chemin du dossier source pour le nouveau job
    /// </summary>
    private string _newJobSourceDirectory = string.Empty;
    public string NewJobSourceDirectory
    {
        get => _newJobSourceDirectory;
        set
        {
            if (value == _newJobSourceDirectory) return;
            _newJobSourceDirectory = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Chemin du dossier destination pour le nouveau job
    /// </summary>
    private string _newJobTargetDirectory = string.Empty;
    public string NewJobTargetDirectory
    {
        get => _newJobTargetDirectory;
        set
        {
            if (value == _newJobTargetDirectory) return;
            _newJobTargetDirectory = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Index du type de backup sélectionné (0=Full, 1=Differential)
    /// </summary>
    private int _newJobTypeIndex;
    public int NewJobTypeIndex
    {
        get => _newJobTypeIndex;
        set
        {
            if (value == _newJobTypeIndex) return;
            _newJobTypeIndex = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// ID du job en cours de modification (null si création)
    /// </summary>
    private int? _editingJobId;
    public int? EditingJobId
    {
        get => _editingJobId;
        private set
        {
            if (value == _editingJobId) return;
            _editingJobId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditing));
        }
    }

    /// <summary>
    /// Indique si un job est en cours de modification
    /// </summary>
    public bool IsEditing => EditingJobId.HasValue;

   

    /// <summary>
    /// Job actuellement sélectionné dans la liste
    /// </summary>
    private BackupJob? _selectedJob;
    public BackupJob? SelectedJob
    {
        get => _selectedJob;
        set
        {
            if (value == _selectedJob) return;
            _selectedJob = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectedJobName));
            OnPropertyChanged(nameof(SelectedJobSource));
            OnPropertyChanged(nameof(SelectedJobTarget));
            OnPropertyChanged(nameof(SelectedJobType));
            SetDefaultCatMessage();
            RefreshSelectedJobState();
        }
    }

    /// <summary>
    /// État d'exécution du job sélectionné (null si pas en cours ou pas de sélection)
    /// </summary>
    private BackupState? _selectedJobState;
    public BackupState? SelectedJobState
    {
        get => _selectedJobState;
        private set
        {
            if (value == _selectedJobState) return;
            _selectedJobState = value;
            OnPropertyChanged();
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
        }
    }

    /// <summary>
    /// Indique si un job est sélectionné
    /// </summary>
    public bool HasSelection => SelectedJob != null;

    /// <summary>
    /// Indique si le job sélectionné est en cours d'exécution
    /// </summary>
    public bool IsJobRunning => SelectedJobState != null && SelectedJobState.Status == BackupStatus.Active;

    /// <summary>
    /// Indique si on doit afficher les détails d'exécution (en cours ou terminé)
    /// </summary>
    public bool ShowExecutionDetails => SelectedJobState != null && 
        (SelectedJobState.Status == BackupStatus.Active || 
         SelectedJobState.Status == BackupStatus.Completed || 
         SelectedJobState.Status == BackupStatus.Error);

    /// <summary>
    /// Indique si le job est terminé (succès ou erreur)
    /// </summary>
    public bool IsJobCompleted => SelectedJobState != null &&
        (SelectedJobState.Status == BackupStatus.Completed ||
         SelectedJobState.Status == BackupStatus.Error);

    // Propriétés du job sélectionné
    public string SelectedJobName => SelectedJob?.Name ?? string.Empty;
    public string SelectedJobSource => SelectedJob?.SourceDirectory ?? string.Empty;
    public string SelectedJobTarget => SelectedJob?.TargetDirectory ?? string.Empty;
    public string SelectedJobType => SelectedJob?.Type.ToString() ?? string.Empty;

    // Propriétés de l'état d'exécution
    public string JobStatusText => SelectedJobState?.Status switch
    {
        BackupStatus.Inactive => Text("GuiStatusInactive"),
        BackupStatus.Active => Text("GuiStatusActive"),
        BackupStatus.Completed => Text("GuiStatusCompleted"),
        BackupStatus.Error => Text("GuiStatusError"),
        _ => Text("GuiStatusInactive")
    };

    /// <summary>
    /// Couleur du texte de statut selon l'état
    /// </summary>
    public string JobStatusColor => SelectedJobState?.Status switch
    {
        BackupStatus.Active => "#3498db",      // Bleu
        BackupStatus.Completed => "#27ae60",   // Vert
        BackupStatus.Error => "#e74c3c",       // Rouge
        _ => "#95a5a6"                          // Gris
    };

    public double JobProgress => SelectedJobState?.ProgressPercentage ?? 0;
    public string JobTotalFiles => SelectedJobState?.TotalFiles.ToString() ?? "-";
    public string JobFilesRemaining => SelectedJobState?.FilesRemaining.ToString() ?? "-";
    public string JobTotalSize => FormatBytes(SelectedJobState?.TotalBytes ?? 0);
    public string JobSizeRemaining => FormatBytes(SelectedJobState?.BytesRemaining ?? 0);
    public string JobCurrentFile => SelectedJobState?.CurrentFileSource ?? "-";
    public string JobLastUpdate => SelectedJobState?.TimeStamp.ToString("HH:mm:ss") ?? "-";
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

    /// <summary>
    /// Formate une taille en octets en format lisible (KB, MB, GB)
    /// </summary>
    private string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;
        
        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }
        
        return $"{size:0.##} {suffixes[suffixIndex]}";
    }

    private void SetCatMessage(string key, params object[] args)
    {
        CatSpeech = args.Length > 0 ? TextFormat(key, args) : Text(key);
    }

    private void SetDefaultCatMessage()
    {
        if (HasSelection)
            SetCatMessage("GuiCatMessageSelected", SelectedJobName);
        else
            SetCatMessage("GuiCatMessageNoSelection");
    }

    /// <summary>
    /// Rafraîchit l'état du job sélectionné
    /// À appeler périodiquement pendant l'exécution d'un job
    /// Lit le fichier state.json pour obtenir l'état en temps réel
    /// </summary>
    private void RefreshSelectedJobState()
    {
        if (SelectedJob == null)
        {
            SelectedJobState = null;
            return;
        }

        try
        {
            // Lit le fichier state.json créé par ProgressJsonWriter
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var statePath = Path.Combine(appData, "EasyLog", "Progress", "state.json");

            if (!File.Exists(statePath))
            {
                SelectedJobState = null;
                return;
            }

            var json = File.ReadAllText(statePath);
            
            // Options de désérialisation pour gérer les constructeurs avec paramètres
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            
            var state = System.Text.Json.JsonSerializer.Deserialize<BackupState>(json, options);

            // Vérifie si l'état correspond au job sélectionné
            if (state?.Job?.Name == SelectedJob.Name)
            {
                SelectedJobState = state;
            }
            else
            {
                SelectedJobState = null;
            }
        }
        catch (Exception ex)
        {
            // Log l'erreur pour debug
            System.Diagnostics.Debug.WriteLine($"Erreur RefreshSelectedJobState: {ex.Message}");
            SelectedJobState = null;
        }
    }

    /// <summary>
    /// Efface l'état d'exécution affiché
    /// </summary>
    public void ClearJobState()
    {
        SelectedJobState = null;
    }

   

    /// <summary>
    /// Crée un nouveau job de sauvegarde de manière asynchrone
    /// </summary>
    public async Task CreateJobAsync()
    {
        // Affichage du statut en cours sur le thread UI
        SetStatusOnUIThread(Text("GuiStatusCreatingJob"));
        SetCatMessage("GuiCatMessageCreating");

        // Capture des valeurs du formulaire pour éviter les problèmes de thread
        var name = NewJobName;
        var src = NewJobSourceDirectory;
        var target = NewJobTargetDirectory;
        var typeIndex = NewJobTypeIndex;

        // Exécution de la création sur un thread séparé
        var (success, message) = await Task.Run(() =>
        {
            var ok = _appViewModel.CreateBackupJob(name, src, target, typeIndex, out var msg);
            return (ok, msg);
        });

        // Mise à jour du statut avec le résultat
        SetStatusOnUIThread(message);

        if (!success)
        {
            SetCatMessage("GuiCatMessageActionFailed", message);
            return;
        }

        // Rafraîchissement de la liste des jobs
        await RefreshJobsAsync();
        SetCatMessage("GuiCatMessageCreated", name);

        // Réinitialisation du formulaire après création réussie
        SetNewJobInputsOnUIThread(string.Empty, string.Empty, string.Empty, 0);
        EditingJobId = null;
    }

    /// <summary>
    /// Charge les données d'un job existant dans le formulaire pour modification
    /// </summary>
    /// <param name="job">Le job à éditer</param>
    public void LoadSelectionForEdit(BackupJob job)
    {
        if (job == null)
        {
            ClearSelectionForEdit();
            return;
        }

        // Calcul de l'ID du job (index + 1)
        var id = BackupJobs.IndexOf(job) + 1;
        if (id <= 0)
        {
            ClearSelectionForEdit();
            return;
        }

        // Chargement des données du job dans le formulaire
        EditingJobId = id;
        NewJobName = job.Name;
        NewJobSourceDirectory = job.SourceDirectory;
        NewJobTargetDirectory = job.TargetDirectory;
        NewJobTypeIndex = job.Type == BackupType.Full ? 0 : 1;
        StatusMessage = Text("GuiStatusEditModeHint");
    }

    /// <summary>
    /// Réinitialise le mode d'édition en annulant les modifications
    /// </summary>
    public void ClearSelectionForEdit()
    {
        EditingJobId = null;
    }

    /// <summary>
    /// Met à jour le job en cours de modification avec les nouvelles données du formulaire
    /// Opération asynchrone exécutée sur un thread séparé
    /// </summary>
    public async Task UpdateSelectedJobAsync()
    {
        // Vérification qu'un job est bien en mode édition
        if (EditingJobId is null)
        {
            SetStatusOnUIThread(Text("GuiErrorSelectSingleToEdit"));
            return;
        }

        SetStatusOnUIThread(Text("GuiStatusUpdatingJob"));
        SetCatMessage("GuiCatMessageUpdating");

        // Capture des valeurs pour le thread séparé
        var id = EditingJobId.Value;
        var src = NewJobSourceDirectory;
        var target = NewJobTargetDirectory;
        var typeIndex = NewJobTypeIndex;

        // Exécution de la mise à jour sur un thread séparé
        var (success, message) = await Task.Run(() =>
        {
            var ok = _appViewModel.UpdateBackupJob(id, src, target, typeIndex, out var msg);
            return (ok, msg);
        });

        SetStatusOnUIThread(message);

        if (!success)
        {
            SetCatMessage("GuiCatMessageActionFailed", message);
            return;
        }

        // Rafraîchissement et sortie du mode édition
        await RefreshJobsAsync();
        SetCatMessage("GuiCatMessageUpdated", SelectedJobName);
        EditingJobId = null;
    }

    /// <summary>
    /// Supprime les jobs sélectionnés de la liste
    /// Traitement multi-sélection avec gestion des erreurs
    /// </summary>
    /// <param name="selectedJobs">Liste des jobs à supprimer</param>
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

        // Conversion des jobs en IDs, tri décroissant pour supprimer de la fin vers le début
        var ids = selectedJobs
            .Select(job => BackupJobs.IndexOf(job) + 1)
            .Where(id => id > 0)
            .Distinct()
            .OrderByDescending(id => id)  // Important : évite les problèmes d'index
            .ToList();

        // Exécution des suppressions sur un thread séparé
        var (deletedCount, errors) = await Task.Run(() =>
        {
            var deleted = 0;
            var errorMessages = new List<string>();
            foreach (var id in ids)
            {
                var ok = _appViewModel.DeleteBackupJob(id, out var msg);
                if (ok)
                    deleted++;
                else
                    errorMessages.Add(msg);
            }

            return (deleted, errorMessages);
        });

        // Rafraîchissement et sortie du mode édition
        await RefreshJobsAsync();
        EditingJobId = null;

        // Affichage du résultat
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
    public Task ExecuteAllAsync()
    {
        if (BackupJobs.Count == 0)
        {
            StatusMessage = Text("GuiErrorNoJobToExecute");
            SetCatMessage("GuiCatMessageNoJobToRun");
            return Task.CompletedTask;
        }

        // Création de l'input "1-N" pour exécuter tous les jobs
        return ExecuteByInputAsync($"1-{BackupJobs.Count}");
    }

    /// <summary>
    /// Exécute uniquement les jobs sélectionnés dans la liste
    /// </summary>
    /// <param name="selectedJobs">Liste des jobs à exécuter</param>
    public Task ExecuteSelectedAsync(IReadOnlyList<BackupJob> selectedJobs)
    {
        if (selectedJobs == null || selectedJobs.Count == 0)
        {
            StatusMessage = Text("GuiErrorNoJobSelected");
            SetDefaultCatMessage();
            return Task.CompletedTask;
        }

        // Conversion des jobs en IDs triés
        var ids = selectedJobs
            .Select(job => BackupJobs.IndexOf(job) + 1)
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (ids.Count == 0)
        {
            StatusMessage = Text("GuiErrorInvalidSelection");
            SetCatMessage("GuiCatMessageInvalidSelection");
            return Task.CompletedTask;
        }

        // Création de l'input formaté (ex: "1;3;5")
        var input = string.Join(';', ids);
        return ExecuteByInputAsync(input);
    }

    /// <summary>
    /// Exécute les jobs spécifiés par une chaîne d'input (ex: "1-5" ou "1;3;5")
    /// Opération asynchrone avec résumé des résultats
    /// </summary>
    /// <param name="input">String d'IDs de jobs (format: "1-5" ou "1;3;5")</param>
    private async Task ExecuteByInputAsync(string input)
    {
        SetStatusOnUIThread(Text("GuiStatusExecuting"));
        SetCatMessage("GuiCatMessageExecuting");

        // Exécution sur un thread séparé
        var (success, results, errorMessage) = await Task.Run(() =>
        {
            var ok = _appViewModel.ExecuteBackupJobs(input, out var res, out var err);
            return (ok, res, err);
        });

        if (!success)
        {
            SetStatusOnUIThread(errorMessage);
            SetCatMessage("GuiCatMessageActionFailed", errorMessage);
            return;
        }

        // Calcul du résumé : nombre total, complétés, erreurs
        var completed = results.Count(r => r.Status == Core.Enums.BackupStatus.Completed);
        var errors = results.Count(r => r.Status == Core.Enums.BackupStatus.Error);
        SetStatusOnUIThread(TextFormat("GuiStatusExecutionSummary", results.Count, completed, errors));

        if (errors == 0)
        {
            SetCatMessage("GuiCatMessageExecutedSuccess", completed);
        }
        else
        {
            SetCatMessage("GuiCatMessageExecutedWithErrors", errors);
        }
    }

  

    /// <summary>
    /// Rafraîchit la liste des jobs en chargeant les données depuis le stockage
    /// Exécute le chargement sur un thread séparé, puis met à jour l'UI sur le thread UI
    /// </summary>
    private async Task RefreshJobsAsync()
    {
        // Chargement des jobs sur un thread séparé
        var jobs = await Task.Run(() => _appViewModel.GetBackupJobs());

        // Mise à jour de l'ObservableCollection sur le thread UI
        if (Dispatcher.UIThread.CheckAccess())
        {
            ReplaceJobs(jobs);
            return;
        }

        Dispatcher.UIThread.Post(() => ReplaceJobs(jobs));
    }

    /// <summary>
    /// Remplace le contenu de la collection BackupJobs avec les nouveaux jobs
    /// DOIT être appelée sur le thread UI uniquement
    /// </summary>
    /// <param name="jobs">Liste des nouveaux jobs</param>
    private void ReplaceJobs(IReadOnlyList<BackupJob> jobs)
    {
        BackupJobs.Clear();
        foreach (var job in jobs)
            BackupJobs.Add(job);
    }

    /// <summary>
    /// Définit les valeurs du formulaire de création de job de manière thread-safe
    /// S'assure que la mise à jour se fait sur le thread UI
    /// </summary>
    private void SetNewJobInputsOnUIThread(string name, string src, string target, int typeIndex)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            NewJobName = name;
            NewJobSourceDirectory = src;
            NewJobTargetDirectory = target;
            NewJobTypeIndex = typeIndex;
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            NewJobName = name;
            NewJobSourceDirectory = src;
            NewJobTargetDirectory = target;
            NewJobTypeIndex = typeIndex;
        });
    }

    /// <summary>
    /// Définit le message de statut de manière thread-safe
    /// S'assure que la mise à jour se fait sur le thread UI
    /// </summary>
    /// <param name="message">Message à afficher dans la barre de statut</param>
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
    /// Méthode publique pour définir le message de statut
    /// Utilisée par les fenêtres enfants (JobEditorWindow)
    /// </summary>
    /// <param name="message">Message à afficher</param>
    public void SetStatus(string message)
    {
        SetStatusOnUIThread(message);
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
    
