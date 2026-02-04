using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
    public class BackupState
    {
        public BackupJob Job { get; }


        public BackupStatus Status { get; set; }
        public long TotalFiles { get; set; }
        public long FilesRemaining { get; set; }
        public long TotalBytes { get; set; }
        public long BytesRemaining { get; set; }
        public string CurrentFileSource { get; set; }
        public string CurrentFileTarget { get; set; }

        public BackupState(BackupJob job)
        {
            Job = job ?? throw new ArgumentNullException(nameof(job));
            Status = BackupStatus.Inactive;
            TotalFiles = 0;
            FilesRemaining = 0;
            TotalBytes = 0;
            BytesRemaining = 0;
            CurrentFileSource = string.Empty;
            CurrentFileTarget = string.Empty;
        }
    }
}
