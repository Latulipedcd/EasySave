using Avalonia.Controls;
using Avalonia.Interactivity;
using Core.Models;
using GUI.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace GUI;

/// <summary>
/// Fenetre principale de l'application EasySave.
/// Code-behind pour MainWindow.axaml : gere les evenements UI et les delegue au ViewModel.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Constructeur de la fenetre principale.
    /// Initialise les composants XAML et configure le DataContext.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    /// <summary>
    /// Gere le clic sur "Executer tout".
    /// Delegue l'action au ViewModel.
    /// </summary>
    private async void ExecuteAll_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        await vm.ExecuteAllAsync();
    }

    /// <summary>
    /// Gere le clic sur "Executer la selection".
    /// Recupere les jobs selectionnes puis delegue l'action au ViewModel.
    /// </summary>
    private async void ExecuteSelected_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var selected = JobsList?.SelectedItems?
            .OfType<BackupJob>()
            .ToList() ?? new List<BackupJob>();

        await vm.ExecuteSelectedAsync(selected);
    }

    /// <summary>
    /// Gere le clic sur "Nouveau job".
    /// Ouvre la fenetre d'edition en mode creation.
    /// </summary>
    private async void NewJob_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        vm.ClearSelectionForEdit();

        var editorWindow = new JobEditorWindow(vm, isEditMode: false);
        await editorWindow.ShowDialog<bool>(this);
    }

    /// <summary>
    /// Gere le clic sur "Modifier le job".
    /// Verifie qu'un seul job est selectionne puis ouvre l'editeur en mode modification.
    /// </summary>
    private async void EditJob_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var selected = JobsList?.SelectedItems?
            .OfType<BackupJob>()
            .ToList();

        if (selected == null || selected.Count != 1)
        {
            vm.SetStatus(vm.GetText("GuiErrorSelectSingleToEdit"));
            return;
        }

        vm.LoadSelectionForEdit(selected[0]);

        var editorWindow = new JobEditorWindow(vm, isEditMode: true);
        var result = await editorWindow.ShowDialog<bool>(this);

        if (!result)
            vm.ClearSelectionForEdit();
    }

    /// <summary>
    /// Gere le changement de selection dans la liste des jobs.
    /// Met a jour le job selectionne dans le ViewModel.
    /// </summary>
    private void JobsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var selected = JobsList?.SelectedItems?
            .OfType<BackupJob>()
            .FirstOrDefault();

        vm.SelectedJob = selected;
    }

    /// <summary>
    /// Gere le clic sur "Supprimer la selection".
    /// Recupere les jobs selectionnes puis delegue la suppression au ViewModel.
    /// </summary>
    private async void DeleteSelected_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var selected = JobsList?.SelectedItems?
            .OfType<BackupJob>()
            .ToList() ?? new List<BackupJob>();

        await vm.DeleteSelectedJobsAsync(selected);
    }

    /// <summary>
    /// Gere le clic sur "Effacer".
    /// Efface l'etat d'execution affiche dans le panneau de details.
    /// </summary>
    private void ClearJobState_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        vm.ClearJobState();
    }
}
