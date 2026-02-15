using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GUI.ViewModels;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private async void ExecuteAll_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        await vm.ExecuteAllAsync();
    }

    private async void ExecuteSelected_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var selected = JobsList?.SelectedItems?
            .OfType<BackupJob>()
            .ToList() ?? new List<BackupJob>();

        await vm.ExecuteSelectedAsync(selected);
    }

    private async void CreateJob_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        await vm.CreateJobAsync();
    }

    private async void UpdateJob_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        await vm.UpdateSelectedJobAsync();
    }

    private async void DeleteSelected_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var selected = JobsList?.SelectedItems?
            .OfType<BackupJob>()
            .ToList() ?? new List<BackupJob>();

        await vm.DeleteSelectedJobsAsync(selected);
    }

    private void JobsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var selected = JobsList?.SelectedItems?
            .OfType<BackupJob>()
            .ToList();

        if (selected == null || selected.Count != 1)
        {
            vm.ClearSelectionForEdit();
            return;
        }

        vm.LoadSelectionForEdit(selected[0]);
    }

    private async void BrowseSource_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choisir le dossier source",
            AllowMultiple = false
        });

        var folder = result.FirstOrDefault();
        var path = folder?.Path.LocalPath;
        if (!string.IsNullOrWhiteSpace(path))
            vm.NewJobSourceDirectory = path;
    }

    private async void BrowseTarget_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choisir le dossier destination",
            AllowMultiple = false
        });

        var folder = result.FirstOrDefault();
        var path = folder?.Path.LocalPath;
        if (!string.IsNullOrWhiteSpace(path))
            vm.NewJobTargetDirectory = path;
    }
}