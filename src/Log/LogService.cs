using System.Reflection.Metadata;



namespace Log
{
    public class LogService
    {
        private static LogService? _instance;

        private ILogWriter writer;

        public static LogService Instance
        {
            get
            {

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
