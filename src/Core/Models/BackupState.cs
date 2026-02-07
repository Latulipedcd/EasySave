using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
    public class BackupState
    {
        public string Job { get; }


        public BackupStatus Status { get; set; }
        public long TotalFiles { get; set; }
        public long FilesRemaining { get; set; }
        public long TotalBytes { get; set; }
        public long BytesRemaining { get; set; }
        public string CurrentFileSource { get; set; }
        public string CurrentFileTarget { get; set; }

        public double ProgressPercentage =>
            TotalBytes == 0 ? 0 :
            (1.0 - (double)BytesRemaining / TotalBytes) * 100;
        

        public BackupState(string job)
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

        public void UpdateProgress()
        {

            if (FilesRemaining == 0 && BytesRemaining == 0)
                Status = BackupStatus.Completed;
            else if (Status == BackupStatus.Inactive)
                Status = BackupStatus.Active;
        }
    }
}