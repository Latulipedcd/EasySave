using Core.Interfaces;
using Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Services
{
    /// <summary>
    /// Writes backup progress state to a JSON file for monitoring and tracking.
    /// Tracks all running job states and persists the full collection as an array.
    /// Thread-safe for concurrent access from parallel backup jobs.
    /// </summary>
    public class ProgressJsonWriter : IProgressWriter
    {
        private readonly string _path;
        private readonly ConcurrentDictionary<string, BackupState> _states = new();
        private readonly object _writeLock = new();
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Initializes a new instance of the ProgressJsonWriter class.
        /// Creates the progress directory if it doesn't exist.
        /// </summary>
        public ProgressJsonWriter()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "EasyLog", "Progress");
            Directory.CreateDirectory(folder);
            _path = Path.Combine(folder, "state.json");
        }

        /// <summary>
        /// Updates the state for the given job and writes all tracked states to the JSON file.
        /// If 3 jobs are running, the file will contain an array of 3 states.
        /// </summary>
        /// <param name="backupState">The backup state to update and persist.</param>
        public void Write(BackupState backupState)
        {
            _states[backupState.Job.Name] = backupState;
            WriteAllStatesToFile();
        }

        /// <summary>
        /// Clears all tracked states and resets the JSON file.
        /// Should be called before starting a new execution batch.
        /// </summary>
        public void Clear()
        {
            _states.Clear();
            WriteAllStatesToFile();
        }

        private void WriteAllStatesToFile()
        {
            lock (_writeLock)
            {
                var json = JsonSerializer.Serialize(_states.Values.ToList(), _jsonOptions);
                var bytes = Encoding.UTF8.GetBytes(json);
                using var fs = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                fs.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
