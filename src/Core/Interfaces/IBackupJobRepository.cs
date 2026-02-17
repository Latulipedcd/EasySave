using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for backup job repository operations (CRUD).
    /// </summary>
    public interface IBackupJobRepository
    {
        /// <summary>
        /// Retrieves all backup jobs.
        /// </summary>
        /// <returns>A read-only list of backup jobs.</returns>
        IReadOnlyList<BackupJob> GetAll();

        /// <summary>
        /// Adds a new backup job.
        /// </summary>
        /// <param name="job">The backup job to add.</param>
        void Add(BackupJob job);

        /// <summary>
        /// Updates an existing backup job.
        /// </summary>
        /// <param name="job">The backup job with updated values.</param>
        void Update(BackupJob job);

        /// <summary>
        /// Deletes a backup job by name.
        /// </summary>
        /// <param name="jobName">The name of the job to delete.</param>
        void Delete(string jobName);
    }
}
