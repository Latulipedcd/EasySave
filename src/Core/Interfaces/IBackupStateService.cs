using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// Manages the full lifecycle of a <see cref="BackupState"/>:
/// initial creation with accurate counters, per-file progress snapshots,
/// error-state creation, and final completion marking.
/// Owns the <see cref="IProgressWriter"/> dependency so that
/// <see cref="IBackupService"/> implementations never call the writer directly.
/// </summary>
public interface IBackupStateService
{
    /// <summary>
    /// Discovers all files under <see cref="BackupJob.SourceDirectory"/>,
    /// sums their sizes, and returns an active <see cref="BackupState"/> together
    /// with the discovered file paths ready for iteration.
    /// </summary>
    (BackupState State, string[] Files) Initialize(BackupJob job);

    /// <summary>
    /// Builds an error <see cref="BackupState"/> with <paramref name="errorMessage"/>,
    /// writes it to the progress channel immediately, and returns it so the
    /// caller can propagate it as its own return value.
    /// </summary>
    BackupState CreateError(BackupJob job, string errorMessage);

    /// <summary>
    /// Snapshots the current counters and file paths into a new
    /// <see cref="BackupState"/> and writes it to the progress channel.
    /// </summary>
    void WriteProgress(BackupJob job, BackupState state);

    /// <summary>
    /// Transitions the state to <see cref="BackupStatus.Completed"/> unless it is
    /// already in an error or cancelled terminal state, then writes a zeroed-out
    /// final snapshot to signal that the job is no longer active.
    /// </summary>
    void Finalize(BackupJob job, BackupState state);
}
