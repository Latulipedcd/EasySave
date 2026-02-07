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
        
        public int FileSize { get; set; }

        public LogEntry()
        {
            BackupName = string.Empty;
            Source = string.Empty;
            Target = string.Empty;
            Duration = TimeSpan.Zero;
            Timestamp = DateTime.MinValue;
            FileSize = 0;
        }

    }
}
