using Core.Models;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// Wrapper pour l'affichage d'un BackupJob avec son ID
/// </summary>
public class BackupJobDisplayItem : ViewModelBase
{
    public BackupJob Job { get; }
    public int Id { get; }
    public string DisplayName { get; }
    public string Name => Job.Name;
    public string SourceDirectory => Job.SourceDirectory;
    public string TargetDirectory => Job.TargetDirectory;
    public string Type => Job.Type.ToString();

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    private double _executionProgress;
    public double ExecutionProgress
    {
        get => _executionProgress;
        set => SetProperty(ref _executionProgress, value);
    }

    private bool _isCompletedSuccess;
    public bool IsCompletedSuccess
    {
        get => _isCompletedSuccess;
        set => SetProperty(ref _isCompletedSuccess, value);
    }

    private bool _hasExecutionError;
    public bool HasExecutionError
    {
        get => _hasExecutionError;
        set => SetProperty(ref _hasExecutionError, value);
    }

    public BackupJobDisplayItem(BackupJob job, int id)
    {
        Job = job;
        Id = id;
        DisplayName = $"{id}. {job.Name}";
        IsRunning = false;
        ExecutionProgress = 0;
        IsCompletedSuccess = false;
        HasExecutionError = false;
    }
}
