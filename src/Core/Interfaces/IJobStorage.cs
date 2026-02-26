using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for providing storage paths for backup job configurations.
    /// Abstracts the physical file locations from the repository layer.
    /// </summary>
    public interface IJobStorage
    {
        /// <summary>
        /// Gets the directory path where backup jobs are stored.
        /// </summary>
        /// <value>The absolute or relative path to the directory containing the job configuration files.</value>
        string JobsDirectory { get; }

        /// <summary>
        /// Gets the full file path for the jobs JSON file.
        /// </summary>
        /// <value>The complete file path, including the directory, file name, and extension (e.g., "C:\Backups\jobs.json").</value>
        string JobsFilePath { get; }
    }
}
