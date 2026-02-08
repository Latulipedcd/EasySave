using Log.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Log.Services
{
    class JsonLogWriter : ILogWriter
    {
        private readonly string _folder;
        string _fileName;
        string _path;

        public JsonLogWriter()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // %APPDATA%\EasyLog\Logs
            _folder = Path.Combine(appData, "EasyLog", "Logs");

            // Crée le dossier s'il n'existe pas
            Directory.CreateDirectory(_folder);

            _fileName = $"log-{DateTime.Now:yyyy-MM-dd}.json";
            _path = Path.Combine(_folder, _fileName);

            Console.WriteLine($"Log file path: {_path}");

        }

        public void Write(Object entry)
        {


            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder); // Ensure the Logs directory exists
            }

            if (!File.Exists(_path))
            {
                File.WriteAllText(_path, "[]"); // Create an empty JSON array if the file doesn't exist
            }

            var json = File.ReadAllText(_path);

            JsonArray array = JsonNode.Parse(json)?.AsArray() ?? new JsonArray(); // Parse existing JSON or create a new array if parsing fails

            JsonNode newEntry = JsonSerializer.SerializeToNode(entry); // Serialize the new log entry to a JsonNode

            array.Add(newEntry); // Add the new entry to the array

            // Write the updated array back to the file with indentation for readability
            File.WriteAllText(_path, array.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
    }
}
