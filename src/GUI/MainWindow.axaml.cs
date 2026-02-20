using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Core.Models;
using EasySave.Presentation.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GUI;

/// <summary>
/// Fenetre principale de l'application EasySave.
/// Code-behind pour MainWindow.axaml : gere les evenements UI et les delegue au ViewModel.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly DataFormat<string> JobDragDataFormat = DataFormat.CreateStringApplicationFormat("EasySave.JobDisplayItem");
    private BackupJobDisplayItem? _dragCandidate;

    /// <summary>
    /// Constructeur de la fenetre principale.
    /// Initialise les composants XAML et configure le DataContext.
    /// </summary>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        if (JobsList != null)
        {
            JobsList.AddHandler(InputElement.PointerPressedEvent, JobsList_PointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
            JobsList.AddHandler(InputElement.PointerMovedEvent, JobsList_PointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
            JobsList.AddHandler(InputElement.PointerReleasedEvent, JobsList_PointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
        }
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
            .OfType<BackupJobDisplayItem>()
            .Select(item => item.Job)
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
            .OfType<BackupJobDisplayItem>()
            .Select(item => item.Job)
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
            .OfType<BackupJobDisplayItem>()
            .Select(item => item.Job)
            .FirstOrDefault();

        vm.SelectedJob = selected;
    }

    private void JobsList_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(JobsList).Properties.IsLeftButtonPressed)
            return;

        _dragCandidate = GetDisplayItemFromSource(e.Source);
    }

    private async void JobsList_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragCandidate == null || !e.GetCurrentPoint(JobsList).Properties.IsLeftButtonPressed)
            return;

        var draggedItem = _dragCandidate;
        _dragCandidate = null;

        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(JobDragDataFormat, draggedItem.Job.Name));

        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
    }

    private void JobsList_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragCandidate = null;
    }

    private void JobsList_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(JobDragDataFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private async void JobsList_Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        if (!e.DataTransfer.Contains(JobDragDataFormat))
            return;

        var draggedJobName = e.DataTransfer.TryGetValue(JobDragDataFormat);
        if (string.IsNullOrWhiteSpace(draggedJobName))
            return;

        var draggedItem = vm.DisplayJobs.FirstOrDefault(item => item.Job.Name == draggedJobName);
        if (draggedItem == null)
            return;

        var targetItem = GetDisplayItemFromSource(e.Source);
        var moved = await vm.MoveJobAsync(draggedItem.Job, targetItem?.Job);
        if (moved)
        {
            var updatedItem = vm.DisplayJobs.FirstOrDefault(item => item.Job == draggedItem.Job);
            if (updatedItem != null && JobsList != null)
            {
                JobsList.SelectedItems?.Clear();
                JobsList.SelectedItem = updatedItem;
            }
        }

        e.Handled = true;
    }

    private static BackupJobDisplayItem? GetDisplayItemFromSource(object? source)
    {
        if (source is not Control control)
            return null;

        if (control.DataContext is BackupJobDisplayItem directItem)
            return directItem;

        if (control is ListBoxItem item && item.DataContext is BackupJobDisplayItem selfItem)
            return selfItem;

        var listBoxItem = control.FindAncestorOfType<ListBoxItem>();
        return listBoxItem?.DataContext as BackupJobDisplayItem;
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
            .OfType<BackupJobDisplayItem>()
            .Select(item => item.Job)
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
