using System;
using Core.Enums;
using Core.Models;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// Represents one job row in the "Run all" progress window.
/// </summary>
public class RunAllJobProgressItemViewModel : ViewModelBase
{
    public int Id { get; }
    public string JobName { get; }
    public string SourceDirectory { get; }
    public string TargetDirectory { get; }
    public string JobType { get; }
    public string DisplayName => $"{Id}. {JobName}";

    private BackupStatus _status = BackupStatus.Inactive;
    public BackupStatus Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    private string _statusText = "-";
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    private string _statusColor = "#95a5a6";
    public string StatusColor
    {
        get => _statusColor;
        private set => SetProperty(ref _statusColor, value);
    }

    private double _progress;
    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    private string _totalFiles = "-";
    public string TotalFiles
    {
        get => _totalFiles;
        private set => SetProperty(ref _totalFiles, value);
    }

    private string _filesRemaining = "-";
    public string FilesRemaining
    {
        get => _filesRemaining;
        private set => SetProperty(ref _filesRemaining, value);
    }

    private string _totalSize = "0 B";
    public string TotalSize
    {
        get => _totalSize;
        private set => SetProperty(ref _totalSize, value);
    }

    private string _sizeRemaining = "0 B";
    public string SizeRemaining
    {
        get => _sizeRemaining;
        private set => SetProperty(ref _sizeRemaining, value);
    }

    private string _currentFile = "-";
    public string CurrentFile
    {
        get => _currentFile;
        private set => SetProperty(ref _currentFile, value);
    }

    private string _lastUpdate = "-";
    public string LastUpdate
    {
        get => _lastUpdate;
        private set => SetProperty(ref _lastUpdate, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (!SetProperty(ref _errorMessage, value))
                return;

            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsCompletedSuccess => Status == BackupStatus.Completed;
    public bool HasExecutionError => Status == BackupStatus.Error;
    public bool HasExecutionCancelled => Status == BackupStatus.Cancelled;
    public bool ShowExecutionDetails => Status != BackupStatus.Inactive;

    public RunAllJobProgressItemViewModel(BackupJobDisplayItem source)
    {
        Id = source.Id;
        JobName = source.Name;
        SourceDirectory = source.SourceDirectory;
        TargetDirectory = source.TargetDirectory;
        JobType = source.Type;
    }

    /// <summary>
    /// Updates the row from a backup state snapshot.
    /// </summary>
    public void ApplyState(BackupState state, Func<string, string> textResolver)
    {
        Status = state.Status;
        StatusText = GetStatusText(state.Status, textResolver);
        StatusColor = GetStatusColor(state.Status);
        Progress = GetProgressPercentage(state.Status, state.ProgressPercentage);
        TotalFiles = state.TotalFiles > 0 ? state.TotalFiles.ToString() : "-";
        FilesRemaining = state.TotalFiles > 0 ? state.FilesRemaining.ToString() : "-";
        TotalSize = FormatBytes(state.TotalBytes);
        SizeRemaining = FormatBytes(state.BytesRemaining);
        CurrentFile = string.IsNullOrWhiteSpace(state.CurrentFileSource) ? "-" : state.CurrentFileSource;
        LastUpdate = FormatTime(state.TimeStamp);
        ErrorMessage = state.ErrorMessage ?? string.Empty;

        OnPropertyChanged(nameof(IsCompletedSuccess));
        OnPropertyChanged(nameof(HasExecutionError));
        OnPropertyChanged(nameof(HasExecutionCancelled));
        OnPropertyChanged(nameof(ShowExecutionDetails));
    }

    private static string GetStatusText(BackupStatus status, Func<string, string> textResolver)
    {
        var key = status switch
        {
            BackupStatus.Active => "GuiStatusActive",
            BackupStatus.Paused => "GuiStatusPaused",
            BackupStatus.Completed => "GuiStatusCompleted",
            BackupStatus.Error => "GuiStatusError",
            BackupStatus.Cancelled => "GuiStatusCancelled",
            _ => "GuiStatusInactive"
        };

        return textResolver(key);
    }

    private static string GetStatusColor(BackupStatus status) => status switch
    {
        BackupStatus.Active => "#3498db",
        BackupStatus.Paused => "#f39c12",
        BackupStatus.Completed => "#27ae60",
        BackupStatus.Error => "#e74c3c",
        BackupStatus.Cancelled => "#e74c3c",
        _ => "#95a5a6"
    };

    private static double GetProgressPercentage(BackupStatus status, double rawProgressPercentage)
    {
        if (status == BackupStatus.Completed)
            return 100;

        return Math.Clamp(rawProgressPercentage, 0, 100);
    }

    private static string FormatBytes(long bytes)
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

    private static string FormatTime(DateTime timeStamp, string format = "HH:mm:ss")
    {
        return timeStamp == default ? "-" : timeStamp.ToString(format);
    }
}
