using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Core.Models
{
    public class LogEntry
    {
        public required string BackupName { get; set; }
     
        public required string Source { get; set; }
        
        public required string Target { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public TimeSpan Duration { get; set; }  

        public DateTime Timestamp { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long FileSize { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public WorkType WorkType { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Encryption time in milliseconds.
        /// 0 = no encryption, >0 = encryption time (ms), <0 = error code
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long EncryptionTimeMs { get; set; }

    }
}
