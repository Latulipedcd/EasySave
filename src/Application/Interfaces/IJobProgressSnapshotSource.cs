using Core.Models;

namespace EasySave.Application.Interfaces;

/// <summary>
/// Application-layer abstraction for reading the latest in-memory progress snapshots.
/// UI layers can depend on this without referencing Core service interfaces.
/// </summary>
public interface IJobProgressSnapshotSource
{
    /// <summary>
    /// Retrieves a read-only snapshot of the current states for all tracked backup jobs.
    /// This allows the UI to poll or refresh progress bars without affecting the underlying execution state.
    /// </summary>
    /// <returns>An <see cref="IReadOnlyList{BackupState}"/> containing 
    /// the most recent progress data for the jobs.</returns>
    IReadOnlyList<BackupState> GetStatesSnapshot();
}
