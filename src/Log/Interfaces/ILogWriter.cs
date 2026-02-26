using System.Text.Json;
using System.Text.Json.Nodes;

namespace Log.Interfaces
{
    /// <summary>
    /// Internal contract for writing serialized log entries to a specific destination.
    /// </summary>
    internal interface ILogWriter
    {
        /// <summary>
        /// Writes the provided log entry object to the underlying storage or stream.
        /// </summary>
        /// <param name="entry">The log entry payload to persist.</param>
        void Write(Object entry);
    }
}

