using Log.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;

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

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            JsonNode newEntry = JsonSerializer.SerializeToNode(entry, options); // Serialize the new log entry to a JsonNode

            array.Add(newEntry); // Add the new entry to the array

            // Write the updated array back to the file with indentation for readability
            var outputJson = JsonSerializer.Serialize(array, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });

            File.WriteAllText(_path, outputJson);
        }
    }

    class XmlLogWriter : ILogWriter
    {
        private readonly string _folder;
        string _fileName;
        string _path;

        public XmlLogWriter()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // %APPDATA%\EasyLog\Logs
            _folder = Path.Combine(appData, "EasyLog", "Logs");

            // Crée le dossier s'il n'existe pas
            Directory.CreateDirectory(_folder);

            _fileName = $"log-{DateTime.Now:yyyy-MM-dd}.xml";
            _path = Path.Combine(_folder, _fileName);

        }

        public void Write(Object entry)
        {
            XDocument doc;

            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder); // Ensure the Logs directory exists
            }

            if (File.Exists(_path))
            {
                doc = XDocument.Load(_path);
            }
            else
            {
                doc = new XDocument(new XElement("Logs"));
            }

            XElement logElement = new XElement("Log",
            new XAttribute("timestamp", DateTime.Now)
        );

            foreach (PropertyInfo prop in entry.GetType().GetProperties())
            {
                object? value = prop.GetValue(entry);

                logElement.Add(
                    new XElement(prop.Name, value?.ToString() ?? "null")
                );
            }

            doc.Root!.Add(logElement);
            doc.Save(_path);
        }
    }
}


