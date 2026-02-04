using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
    public class LogEntry
    {
        private String _backupName;
        public String BackupName
        {
            get { return _backupName; }
            set { _backupName = value; }
        }
        private string _source;
        public string Source
        {
            get { return _source; }
            set { _source = value; }
        }
        private string _target;
        public string Target
        {
            get { return _target; }
            set { _target = value; }
        }
        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get { return _duration; }
            set { _duration = value; }
        }
        private DateTime _timestamp;
        public DateTime Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }
        private int _fileSize;
        public int FileSize
        {
            get { return _fileSize; }
            set { _fileSize = value; }
        }

        public LogEntry()
        {
            _backupName = string.Empty;
            _source = string.Empty;
            _target = string.Empty;
            _duration = TimeSpan.Zero;
            _timestamp = DateTime.MinValue;
            _fileSize = 0;
        }

    }
}
