using Core.Models;

namespace EasySave.Application.Interfaces;

/// <summary>
/// Reads the current backup job progress states from persistent storage.
/// </summary>
public interface IJobStateReader
{
    /// <summary>
    /// Returns all job states from the progress state file, or null if unavailable.
    /// </summary>
    List<BackupState>? ReadAllStates();
}
