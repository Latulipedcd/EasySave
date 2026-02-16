using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GUI.ViewModels;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GUI;

/// <summary>
/// Fenêtre principale de l'application EasySave
/// Code-behind pour MainWindow.axaml - Gère les événements de l'UI et les délègue au ViewModel
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Constructeur de la fenêtre principale
    /// Initialise les composants XAML et configure le DataContext avec le ViewModel
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

   

    /// <summary>
    /// Gère le clic sur le bouton "Exécuter Tous"
    /// Délègue l'action au ViewModel pour exécuter tous les jobs
    /// </summary>
    private async void ExecuteAll_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        await vm.ExecuteAllAsync();
    }

    /// <summary>
    /// Gère le clic sur le bouton "Exécuter Sélection"
    /// Récupère les jobs sélectionnés et délègue l'action au ViewModel
    /// </summary>
    private async void ExecuteSelected_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        // Récupération des jobs sélectionnés dans la ListBox
        var selected = JobsList?.SelectedItems?
            .OfType<BackupJob>()
            .ToList() ?? new List<BackupJob>();

        await vm.ExecuteSelectedAsync(selected);
    }


    /// <summary>
    /// Gère le clic sur le bouton "Nouveau job"
    /// Ouvre une fenêtre popup pour créer un nouveau job
    /// </summary>
    private async void NewJob_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        // Réinitialise le formulaire pour un nouveau job
        vm.ClearSelectionForEdit();

        // Ouvre la fenêtre popup en mode création
        var editorWindow = new JobEditorWindow(vm, isEditMode: false);

        // Affiche la popup de manière modale et attend le résultat
        var result = await editorWindow.ShowDialog<bool>(this);

        // Si l'utilisateur a sauvegardé, rafraîchit la liste
        if (result)
        {
            // La liste sera rafraîchie automatiquement par CreateJobAsync
        }
    }

    /// <summary>
    /// Gère le clic sur le bouton "Modifier le job"
    /// Ouvre une fenêtre popup pour modifier le job sélectionné
    /// </summary>
    private async void EditJob_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        // Récupère le job sélectionné
        var selected = JobsList?.SelectedItems?
            .OfType<BackupJob>()
            .ToList();

        // Vérifie qu'un seul job est sélectionné
        if (selected == null || selected.Count != 1)
        {
            vm.SetStatus(vm.GetText("GuiErrorSelectSingleToEdit"));
            return;
        }

        // Charge le job dans le formulaire pour édition
        vm.LoadSelectionForEdit(selected[0]);

        // Ouvre la fenêtre popup en mode édition
        var editorWindow = new JobEditorWindow(vm, isEditMode: true);

        // Affiche la popup de manière modale et attend le résultat
        var result = await editorWindow.ShowDialog<bool>(this);

        // Si l'utilisateur a sauvegardé, rafraîchit la liste
        if (result)
        {
            // La liste sera rafraîchie automatiquement par UpdateSelectedJobAsync
        }
        else
        {
            // Si annulé, réinitialise le mode édition
            vm.ClearSelectionForEdit();
        }
    }

   

    /// <summary>
    /// Gère le changement de sélection dans la liste des jobs
    /// Met à jour le panneau de détails avec les informations du job sélectionné
    /// </summary>
    private void JobsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        // Récupère le premier job sélectionné pour affichage des détails
        var selected = JobsList?.SelectedItems?
            .OfType<BackupJob>()
            .FirstOrDefault();

        vm.SelectedJob = selected;
    }

    /// <summary>
    /// Gère le clic sur le bouton "Supprimer"
    /// Récupère les jobs sélectionnés et délègue l'action au ViewModel
    /// </summary>
    private async void DeleteSelected_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        // Récupération des jobs sélectionnés
        var selected = JobsList?.SelectedItems?
            .OfType<BackupJob>()
            .ToList() ?? new List<BackupJob>();

        await vm.DeleteSelectedJobsAsync(selected);
    }

    /// <summary>
    /// Gère le clic sur le bouton "Effacer"
    /// Efface l'état d'exécution affiché
    /// </summary>
    private void ClearJobState_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        vm.ClearJobState();
    }

}
