using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
    public class BackupJob
    {
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        public BackupType Type { get; set; }


        /// <summary>
        /// Initializes a new instance of a backup job, validating that all required paths and names are provided.
        /// </summary>
        /// <param name="name">The unique identifier or display name for the job.</param>
        /// <param name="sourceDirectory">The absolute path of the origin directory to back up.</param>
        /// <param name="targetDirectory">The absolute path of the destination directory.</param>
        /// <param name="type">The type of backup to perform (e.g., Full or Differential).</param>
        /// <exception cref="ArgumentException">Thrown when the name, source directory, or target directory are null or whitespace.</exception>
        [System.Text.Json.Serialization.JsonConstructor]
        public BackupJob(
            string name,
            string sourceDirectory,
            string targetDirectory,
            BackupType type)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Backup name cannot be empty");

            if (string.IsNullOrWhiteSpace(sourceDirectory))
                throw new ArgumentException("Source directory cannot be empty");

            if (string.IsNullOrWhiteSpace(targetDirectory))
                throw new ArgumentException("Target directory cannot be empty");

            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
        }
    }
}
