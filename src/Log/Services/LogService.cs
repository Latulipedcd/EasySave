using Log.Interfaces;
using System.Reflection.Metadata;



namespace Log.Services
{
    public class LogService: ILog
    {
        private static LogService? _instance; // Singleton instance

        private ILogWriter writer;

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
            writer = new JsonLogWriter(); 
        }

        public void LogBackup(Object entry)
        {
            writer.Write(entry);
        }

    }
}
