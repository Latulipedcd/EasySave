using Core.Enums;
using Core.Interfaces;
using Core.Models;
using System;
using System.IO;
using System.Linq;

namespace Core.Services;

/// <summary>
/// Default implementation of <see cref="IBackupStateService"/>.
/// Uses <see cref="IFileService"/> for file discovery and
/// <see cref="IProgressWriter"/> for all state persistence, keeping
/// both concerns out of the backup orchestrator.
/// </summary>
public class BackupStateService : IBackupStateService
{
    private readonly IFileService _fileService;
    private readonly IProgressWriter _progressWriter;

    public BackupStateService(IFileService fileService, IProgressWriter progressWriter)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _progressWriter = progressWriter ?? throw new ArgumentNullException(nameof(progressWriter));
    }

    /// <summary>
    /// Enumerates all files under the job's source directory, sums their sizes,
    /// and returns a fully initialised active <see cref="BackupState"/> together
    /// with the discovered file paths so the caller can iterate them.
    /// </summary>
    public (BackupState State, string[] Files) Initialize(BackupJob job)
    {
        var files = _fileService.GetFiles(job.SourceDirectory);
        long totalBytes = files.Sum(f => new FileInfo(f).Length);

        var state = new BackupState(job)
        {
            Status = BackupStatus.Active,
            TotalFiles = files.Length,
            FilesRemaining = files.Length,
            TotalBytes = totalBytes,
            BytesRemaining = totalBytes
        };

        return (state, files);
    }

    /// <summary>
    /// Builds a <see cref="BackupStatus.Error"/> state with the supplied message,
    /// writes it to the progress channel immediately so listeners are notified,
    /// and returns it so callers can propagate it as their own return value.
    /// </summary>
    public BackupState CreateError(BackupJob job, string errorMessage)
    {
        var errorState = new BackupState(job)
        {
            Status = BackupStatus.Error,
            TimeStamp = DateTime.Now,
            ErrorMessage = errorMessage
        };
        _progressWriter.Write(errorState);
        return errorState;
    }

    /// <summary>
    /// Copies the current counters and file paths into a new snapshot and
    /// writes it to the progress channel so the UI reflects the latest file processed.
    /// </summary>
    public void WriteProgress(BackupJob job, BackupState state)
    {
        _progressWriter.Write(new BackupState(job)
        {
            Status = state.Status,
            TimeStamp = DateTime.Now,
            TotalFiles = state.TotalFiles,
            FilesRemaining = state.FilesRemaining,
            TotalBytes = state.TotalBytes,
            BytesRemaining = state.BytesRemaining,
            CurrentFileSource = state.CurrentFileSource,
            CurrentFileTarget = state.CurrentFileTarget
        });
    }

    /// <summary>
    /// Marks the state as <see cref="BackupStatus.Completed"/> when it has not
    /// already reached an error or cancelled terminal state, then writes a
    /// zeroed-out final snapshot to signal that the job slot is free.
    /// </summary>
    public void Finalize(BackupJob job, BackupState state)
    {
        if (state.Status != BackupStatus.Error && state.Status != BackupStatus.Cancelled)
            state.Status = BackupStatus.Completed;

        _progressWriter.Write(new BackupState(job)
        {
            Status = state.Status,
            TimeStamp = DateTime.Now,
            TotalFiles = 0,
            FilesRemaining = 0,
            TotalBytes = 0,
            BytesRemaining = 0,
            CurrentFileSource = null,
            CurrentFileTarget = null
        });
    }
}
