using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
    public class LogEntry
    {
        public required string BackupName { get; set; }
     
        public required string Source { get; set; }
        
        public required string Target { get; set; }
       
        public TimeSpan Duration { get; set; }  

        public DateTime Timestamp { get; set; }
        
        public long FileSize { get; set; }

        public WorkType WorkType { get; set; }
    }
}
