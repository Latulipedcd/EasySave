using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GUI.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GUI;

/// <summary>
/// Fenêtre popup pour créer ou modifier un job de backup
/// Fenêtre modale qui retourne true si sauvegardé, false si annulé
/// </summary>
public partial class JobEditorWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly bool _isEditMode;

    /// <summary>
    /// Constructeur pour la création d'un nouveau job
    /// </summary>
    /// <param name="viewModel">ViewModel partagé avec la fenêtre principale</param>
    public JobEditorWindow(MainWindowViewModel viewModel) : this(viewModel, false)
    {
    }

    /// <summary>
    /// Constructeur avec mode spécifique (création ou édition)
    /// </summary>
    /// <param name="viewModel">ViewModel partagé</param>
    /// <param name="isEditMode">True pour édition, False pour création</param>
    public JobEditorWindow(MainWindowViewModel viewModel, bool isEditMode)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _isEditMode = isEditMode;
        DataContext = _viewModel;
    }

    /// <summary>
    /// Gère le clic sur le bouton "Sauvegarder"
    /// Déclenche la création ou mise à jour du job
    /// </summary>
    private async void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var saved = false;

            if (_isEditMode)
            {
                saved = await _viewModel.UpdateSelectedJobAsync();
            }
            else
            {
                saved = await _viewModel.CreateJobAsync();
            }

            if (saved)
            {
                // Ferme la fenêtre avec succès
                Close(true);
            }
        }
        catch (Exception ex)
        {
            // En cas d'erreur, affiche un message et garde la fenêtre ouverte
            _viewModel.SetStatus($"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Gère le clic sur le bouton "Annuler"
    /// Ferme la fenêtre sans sauvegarder
    /// </summary>
    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }

    /// <summary>
    /// Gère le clic sur le bouton "Parcourir" pour le dossier source
    /// Ouvre un dialogue de sélection de dossier
    /// </summary>
    private async void BrowseSource_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = _viewModel.GetText("GuiPickSourceTitle"),
            AllowMultiple = false
        });

        var folder = result.FirstOrDefault();
        var path = folder?.Path.LocalPath;
        if (!string.IsNullOrWhiteSpace(path))
            _viewModel.NewJobSourceDirectory = path;
    }

    /// <summary>
    /// Gère le clic sur le bouton "Parcourir" pour le dossier destination
    /// Ouvre un dialogue de sélection de dossier
    /// </summary>
    private async void BrowseTarget_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = _viewModel.GetText("GuiPickDestinationTitle"),
            AllowMultiple = false
        });

        var folder = result.FirstOrDefault();
        var path = folder?.Path.LocalPath;
        if (!string.IsNullOrWhiteSpace(path))
            _viewModel.NewJobTargetDirectory = path;
    }
}
