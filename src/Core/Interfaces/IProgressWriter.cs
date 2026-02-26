using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for writing backup progress state to persistent storage.
    /// </summary>
    public interface IProgressWriter
    {
        /// <summary>
        /// Writes or updates the backup state for a specific job.
        /// The writer tracks the states of all active jobs and persists the full collection to storage.
        /// </summary>
        /// <param name="backupState">The current backup state snapshot to persist.</param>
        void Write(BackupState backupState);

        /// <summary>
        /// Clears all currently tracked job states and resets the persistent storage.
        /// Typically called before starting a fresh batch of backup jobs to ensure stale data is removed.
        /// </summary>
        void Clear();
    }
}
