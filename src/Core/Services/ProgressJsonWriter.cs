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

    public class ProgressJsonWriter :  IProgressWriter
    {
        private readonly string _appData;
        private readonly string _folder;
        private readonly string _path;

        public ProgressJsonWriter()
        {
            _appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _folder = Path.Combine(_appData, "EasyLog", "Progress");
            Directory.CreateDirectory(_folder);
            _path = Path.Combine(_folder, "state.json");
        }

        public void Write(BackupState backupState)
        {
            
            // Serialize the backup state to JSON with indented formatting and enum as string
            var json = JsonSerializer.Serialize(backupState, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });

            File.WriteAllText(_path, json);
        }
    }
}
