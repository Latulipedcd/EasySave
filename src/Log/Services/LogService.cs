using Log.Enums;
using Log.Factory;
using Log.Interfaces;
using System.Reflection.Metadata;



namespace Log.Services
{
    public class LogService: ILog
    {
        private static LogService? _instance; // Singleton instance

        private ILogWriter _writer;

        public static LogService Instance
        {
            get
            {
                // Double-checked locking to ensure thread safety
                if (_instance == null)
                {
                    _instance = new LogService();
                }
                return _instance;
            }
        }

        private LogService()
        {
            _writer = LogWriterFactory.Create(LogFormat.Json); // Json by default for retrocompatibility
        }

        public void Configure(LogFormat format)
        {
            _writer = LogWriterFactory.Create(format);
        }

        public void LogBackup(Object entry)
        {
            _writer.Write(entry);
        }

    }
}
