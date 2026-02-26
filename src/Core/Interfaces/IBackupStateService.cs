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
    /// <param name="job">The backup job configuration containing the source directory to scan.</param>
    /// <returns>A tuple containing the initialized <see cref="BackupState"/> and an array of discovered file paths.</returns>
    (BackupState State, string[] Files) Initialize(BackupJob job);

    /// <summary>
    /// Builds an error <see cref="BackupState"/> with <paramref name="errorMessage"/>,
    /// writes it to the progress channel immediately, and returns it so the
    /// caller can propagate it as its own return value.
    /// </summary>
    /// <param name="job">The backup job that encountered the error.</param>
    /// <param name="errorMessage">The descriptive message detailing why the backup failed.</param>
    /// <returns>A newly created <see cref="BackupState"/> reflecting the error condition.</returns>
    BackupState CreateError(BackupJob job, string errorMessage);

    /// <summary>
    /// Snapshots the current counters and file paths into a new
    /// <see cref="BackupState"/> and writes it to the progress channel.
    /// </summary>
    /// <param name="job">The backup job currently being executed.</param>
    /// <param name="state">The current state containing updated progress counters and the current file being processed.</param>
    void WriteProgress(BackupJob job, BackupState state);

    /// <summary>
    /// Transitions the state to <see cref="BackupStatus.Completed"/> unless it is
    /// already in an error or cancelled terminal state, then writes a zeroed-out
    /// final snapshot to signal that the job is no longer active.
    /// </summary>
    /// <param name="job">The backup job that has finished execution.</param>
    /// <param name="state">The final state to be evaluated and marked as completed, cancelled, or errored.</param>
    void Finalize(BackupJob job, BackupState state);
}
