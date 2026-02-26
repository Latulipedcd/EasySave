using Log.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Log.Interfaces
{
    /// <summary>
    /// Low-level logging contract for configuring and writing serialized backup log entries.
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Configures the output format for subsequent log writes.
        /// </summary>
        /// <param name="format">The target format (e.g., JSON or XML).</param>
        void Configure(LogFormat format);

        /// <summary>
        /// Serializes and writes a generic log entry object to the configured storage.
        /// </summary>
        /// <param name="entry">The log entry data model to persist.</param>
        void LogBackup(Object entry);
    }
}
