using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for providing storage paths for backup jobs.
    /// </summary>
    public interface IJobStorage
    {
        /// <summary>
        /// Gets the directory path where backup jobs are stored.
        /// </summary>
        string JobsDirectory { get; }

        /// <summary>
        /// Gets the full file path for the jobs JSON file.
        /// </summary>
        string JobsFilePath { get; }
    }
}
