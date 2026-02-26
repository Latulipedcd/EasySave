using Core.Models;
using System;
using System.Collections.Generic;

namespace Core.Interfaces
{
    /// <summary>
    /// Provides read access to the current in-memory progress state snapshot.
    /// Useful for in-process UIs that should not poll the state.json file.
    /// </summary>
    public interface IProgressSnapshotSource
    {
        /// <summary>
        /// Returns a point-in-time snapshot of all known job states.
        /// </summary>
        IReadOnlyList<BackupState> GetStatesSnapshot();
    }
}
