using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    /// <summary>
    /// Provides storage paths for backup job persistence.
    /// </summary>
    public class JobStorage : IJobStorage
    {
        /// <summary>
        /// Gets the directory path where backup jobs are stored.
        /// Located in AppData\Roaming\EasySave\Jobs.
        /// </summary>
        public string JobsDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave",
                "Jobs");

        /// <summary>
        /// Gets the full file path for the jobs JSON file.
        /// </summary>
        public string JobsFilePath =>
            Path.Combine(JobsDirectory, "jobs.json");
    }

}
