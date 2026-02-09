using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
    public class LogEntry
    {
        public String BackupName { get; set; }
     
        public string Source { get; set; }
        
        public string Target { get; set; }
       
        public TimeSpan Duration { get; set; }  

        public DateTime Timestamp { get; set; }
        
        public long FileSize { get; set; }

        public WorkType WorkType { get; set; }

        public LogEntry()
        {
            }

    }
}
