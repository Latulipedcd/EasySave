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
        /// Writes/updates the backup state for a specific job.
        /// The writer tracks all job states and persists the full collection.
        /// </summary>
        /// <param name="backupState">The backup state to write.</param>
        void Write(BackupState backupState);

        /// <summary>
        /// Clears all tracked job states and resets the persistent storage.
        /// Should be called before starting a new batch of jobs.
        /// </summary>
        void Clear();
    }
}
