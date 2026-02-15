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

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly MainViewModel _appViewModel;

    public ObservableCollection<BackupJob> BackupJobs { get; }

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

    public MainWindowViewModel()
    {
        _appViewModel = new MainViewModel();
        BackupJobs = new ObservableCollection<BackupJob>(_appViewModel.GetBackupJobs());
        _newJobTypeIndex = 0;
    }

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

    public bool IsEditing => EditingJobId.HasValue;

    public async Task CreateJobAsync()
    {
        SetStatusOnUIThread("Création du job...");

        var name = NewJobName;
        var src = NewJobSourceDirectory;
        var target = NewJobTargetDirectory;
        var typeIndex = NewJobTypeIndex;

        var (success, message) = await Task.Run(() =>
        {
            var ok = _appViewModel.CreateBackupJob(name, src, target, typeIndex, out var msg);
            return (ok, msg);
        });

        SetStatusOnUIThread(message);

        if (!success)
            return;

        await RefreshJobsAsync();

        // Clear inputs after successful creation
        SetNewJobInputsOnUIThread(string.Empty, string.Empty, string.Empty, 0);
        EditingJobId = null;
    }

    public void LoadSelectionForEdit(BackupJob job)
    {
        if (job == null)
        {
            ClearSelectionForEdit();
            return;
        }

        var id = BackupJobs.IndexOf(job) + 1;
        if (id <= 0)
        {
            ClearSelectionForEdit();
            return;
        }

        EditingJobId = id;
        NewJobName = job.Name;
        NewJobSourceDirectory = job.SourceDirectory;
        NewJobTargetDirectory = job.TargetDirectory;
        NewJobTypeIndex = job.Type == BackupType.Full ? 0 : 1;
        StatusMessage = "Mode modification : changez les champs puis cliquez sur Modifier.";
    }

    public void ClearSelectionForEdit()
    {
        EditingJobId = null;
    }

    public async Task UpdateSelectedJobAsync()
    {
        if (EditingJobId is null)
        {
            SetStatusOnUIThread("Sélectionnez un job (un seul) pour modifier.");
            return;
        }

        SetStatusOnUIThread("Modification du job...");

        var id = EditingJobId.Value;
        var src = NewJobSourceDirectory;
        var target = NewJobTargetDirectory;
        var typeIndex = NewJobTypeIndex;

        var (success, message) = await Task.Run(() =>
        {
            var ok = _appViewModel.UpdateBackupJob(id, src, target, typeIndex, out var msg);
            return (ok, msg);
        });

        SetStatusOnUIThread(message);

        if (!success)
            return;

        await RefreshJobsAsync();
        EditingJobId = null;
    }

    public async Task DeleteSelectedJobsAsync(IReadOnlyList<BackupJob> selectedJobs)
    {
        if (selectedJobs == null || selectedJobs.Count == 0)
        {
            SetStatusOnUIThread("Aucun job sélectionné.");
            return;
        }

        SetStatusOnUIThread("Suppression...");

        var ids = selectedJobs
            .Select(job => BackupJobs.IndexOf(job) + 1)
            .Where(id => id > 0)
            .Distinct()
            .OrderByDescending(id => id)
            .ToList();

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

        await RefreshJobsAsync();
        EditingJobId = null;

        if (errors.Count == 0)
        {
            SetStatusOnUIThread($"{deletedCount} job(s) supprimé(s)." );
            return;
        }

        SetStatusOnUIThread($"{deletedCount} supprimé(s), {errors.Count} erreur(s) : {errors[0]}");
    }

    public Task ExecuteAllAsync()
    {
        if (BackupJobs.Count == 0)
        {
            StatusMessage = "Aucun job à exécuter.";
            return Task.CompletedTask;
        }

        return ExecuteByInputAsync($"1-{BackupJobs.Count}");
    }

    public Task ExecuteSelectedAsync(IReadOnlyList<BackupJob> selectedJobs)
    {
        if (selectedJobs == null || selectedJobs.Count == 0)
        {
            StatusMessage = "Aucun job sélectionné.";
            return Task.CompletedTask;
        }

        var ids = selectedJobs
            .Select(job => BackupJobs.IndexOf(job) + 1)
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (ids.Count == 0)
        {
            StatusMessage = "Sélection invalide.";
            return Task.CompletedTask;
        }

        var input = string.Join(';', ids);
        return ExecuteByInputAsync(input);
    }

    private async Task ExecuteByInputAsync(string input)
    {
        SetStatusOnUIThread("Exécution...");

        var (success, results, errorMessage) = await Task.Run(() =>
        {
            var ok = _appViewModel.ExecuteBackupJobs(input, out var res, out var err);
            return (ok, res, err);
        });

        if (!success)
        {
            SetStatusOnUIThread(errorMessage);
            return;
        }

        var completed = results.Count(r => r.Status == Core.Enums.BackupStatus.Completed);
        var errors = results.Count(r => r.Status == Core.Enums.BackupStatus.Error);
        SetStatusOnUIThread($"{results.Count} job(s) exécuté(s) : {completed} terminé(s), {errors} erreur(s).");
    }

    private async Task RefreshJobsAsync()
    {
        var jobs = await Task.Run(() => _appViewModel.GetBackupJobs());

        if (Dispatcher.UIThread.CheckAccess())
        {
            ReplaceJobs(jobs);
            return;
        }

        Dispatcher.UIThread.Post(() => ReplaceJobs(jobs));
    }

    private void ReplaceJobs(IReadOnlyList<BackupJob> jobs)
    {
        BackupJobs.Clear();
        foreach (var job in jobs)
            BackupJobs.Add(job);
    }

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

    private void SetStatusOnUIThread(string message)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            StatusMessage = message;
            return;
        }

        Dispatcher.UIThread.Post(() => StatusMessage = message);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
