using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Services
{
    using Core.Interfaces;
    using System.Diagnostics;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Writes backup progress state to a JSON file for monitoring and tracking.
    /// </summary>
    public class ProgressJsonWriter :  IProgressWriter
    {
        private readonly string _appData;
        private readonly string _folder;
        private readonly string _path;

        /// <summary>
        /// Initializes a new instance of the ProgressJsonWriter class.
        /// Creates the progress directory if it doesn't exist.
        /// </summary>
        public ProgressJsonWriter()
        {
            _appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _folder = Path.Combine(_appData, "EasyLog", "Progress");
            Directory.CreateDirectory(_folder);
            _path = Path.Combine(_folder, "state.json");
        }

        /// <summary>
        /// Writes the backup state to a JSON file with indented formatting.
        /// The file is located at AppData\Roaming\EasyLog\Progress\state.json.
        /// </summary>
        /// <param name="backupState">The backup state to serialize and write.</param>
        public void Write(BackupState backupState)
        {
            
            // Serialize the backup state to JSON with indented formatting and enum as string
            var json = JsonSerializer.Serialize(backupState, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });

            File.WriteAllText(_path, json);
        }
    }
}
