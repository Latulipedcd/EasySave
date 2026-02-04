using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
    public class BackupJob
    {
        public string Name { get; }
        public string SourceDirectory { get; }
        public string TargetDirectory { get; }
        public BackupType Type { get; }

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
