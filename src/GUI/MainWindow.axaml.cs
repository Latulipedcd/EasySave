using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Core.Models;
using EasySave.Presentation.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
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

        if (selected.Count > 0)
        {
            var confirmed = await ShowDeleteConfirmationAsync(vm, selected);
            if (!confirmed)
            {
                vm.SetStatus(vm.GetText("GuiStatusDeleteCancelled"));
                return;
            }
        }

        await vm.DeleteSelectedJobsAsync(selected);
    }

    private async Task<bool> ShowDeleteConfirmationAsync(MainWindowViewModel vm, IReadOnlyList<BackupJob> selectedJobs)
    {
        var selectedNames = selectedJobs
            .Select(job => job.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();

        var message = selectedNames.Count == 1
            ? string.Format(vm.GetText("GuiDeleteConfirmMessageSingle"), selectedNames[0])
            : string.Format(vm.GetText("GuiDeleteConfirmMessageMultiple"), selectedNames.Count);

        var jobNamesText = string.Join(Environment.NewLine, selectedNames.Select(name => $"• {name}"));

        var cancelButton = new Button
        {
            Content = vm.GetText("GuiDeleteConfirmNo"),
            MinWidth = 100
        };
        cancelButton.Classes.Add("secondary");

        var confirmButton = new Button
        {
            Content = vm.GetText("GuiDeleteConfirmYes"),
            MinWidth = 100
        };
        confirmButton.Classes.Add("danger");

        var dialog = new Window
        {
            Title = vm.GetText("GuiDeleteConfirmTitle"),
            Width = 520,
            Height = 310,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Classes = { "card" },
                Margin = new Avalonia.Thickness(14),
                Child = new StackPanel
                {
                    Spacing = 14,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            Classes = { "subtitle" },
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        },
                        new TextBlock
                        {
                            Text = vm.GetText("GuiDeleteConfirmJobsLabel"),
                            Classes = { "label" }
                        },
                        new Border
                        {
                            BorderBrush = Avalonia.Media.Brushes.Gray,
                            BorderThickness = new Avalonia.Thickness(1),
                            CornerRadius = new Avalonia.CornerRadius(8),
                            Padding = new Avalonia.Thickness(10),
                            MaxHeight = 130,
                            Child = new ScrollViewer
                            {
                                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                                Content = new TextBlock
                                {
                                    Text = string.IsNullOrWhiteSpace(jobNamesText) ? "-" : jobNamesText,
                                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                                }
                            }
                        },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Spacing = 10,
                            Children =
                            {
                                cancelButton,
                                confirmButton
                            }
                        }
                    }
                }
            }
        };

        var completion = new TaskCompletionSource<bool>();

        cancelButton.Click += (_, _) =>
        {
            completion.TrySetResult(false);
            dialog.Close();
        };

        confirmButton.Click += (_, _) =>
        {
            completion.TrySetResult(true);
            dialog.Close();
        };

        dialog.Closed += (_, _) => completion.TrySetResult(false);

        await dialog.ShowDialog(this);
        return await completion.Task;
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
