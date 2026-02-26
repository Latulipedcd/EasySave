using Core.Models;
using Log.Enums;

namespace Core.Interfaces
{
    /// <summary>
    /// Service responsible for establishing a connection to a Docker-based logging container 
    /// or service and transmitting structured log entries.
    /// </summary>
    public interface IDockerLoggerService
    {
        /// <summary>
        /// Establishes the necessary network or socket connection to the Docker logging service.
        /// Should typically be called before attempting to send any logs.
        /// </summary>
        public void Connect();

        /// <summary>
        /// Serializes and transmits a single log entry to the connected Docker service.
        /// </summary>
        /// <param name="format">The target format (e.g., JSON or XML) to serialize the log entry into before transmission.</param>
        /// <param name="entry">The structured log entry containing the specific details of the backup event to record.</param>
        public void SendLog(LogFormat format, LogEntry entry);

    }
}
