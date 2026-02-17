using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Core.Models
{
    public class BackupState
    {
        public BackupJob Job { get; set; }


        public BackupStatus Status { get; set; }
        public DateTime TimeStamp { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long TotalFiles { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long FilesRemaining { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long TotalBytes { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long BytesRemaining { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? CurrentFileSource { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? CurrentFileTarget { get; set; }

        [JsonIgnore]
        public double ProgressPercentage =>
            TotalBytes == 0 ? 0 :
            (1.0 - (double)BytesRemaining / TotalBytes) * 100;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? ErrorMessage { get; set; }


        [JsonConstructor]
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

        public void UpdateProgress()
        {

            if (FilesRemaining == 0 && BytesRemaining == 0)
                Status = BackupStatus.Completed;
            else if (Status == BackupStatus.Inactive)
                Status = BackupStatus.Active;
        }
    }
}