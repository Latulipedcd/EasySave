using Log.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
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
        private static readonly object _fileLock = new();

        private static readonly JsonSerializerOptions _serializeOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public JsonLogWriter()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _folder = Path.Combine(appData, "EasyLog", "Logs");
            Directory.CreateDirectory(_folder);
            _fileName = $"log-{DateTime.Now:yyyy-MM-dd}.json";
            _path = Path.Combine(_folder, _fileName);
        }

        public void Write(Object entry)
        {
            lock (_fileLock)
            {
                if (!Directory.Exists(_folder))
                {
                    Directory.CreateDirectory(_folder);
                }

                string json;
                if (!File.Exists(_path))
                {
                    json = "[]";
                }
                else
                {
                    using var readStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(readStream, Encoding.UTF8);
                    json = reader.ReadToEnd();
                }

                JsonArray array;
                try
                {
                    array = JsonNode.Parse(json)?.AsArray() ?? new JsonArray();
                }
                catch (JsonException)
                {
                    // Log file was corrupted (e.g. from a previous crash) — start fresh
                    array = new JsonArray();
                }

                JsonNode? newEntry = JsonSerializer.SerializeToNode(entry, _serializeOptions);
                array.Add(newEntry);

                var outputJson = JsonSerializer.Serialize(array, _serializeOptions);

                var bytes = Encoding.UTF8.GetBytes(outputJson);
                using var writeStream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                writeStream.Write(bytes, 0, bytes.Length);
            }
        }
    }

    class XmlLogWriter : ILogWriter
    {
        private readonly string _folder;
        string _fileName;
        string _path;
        private static readonly object _fileLock = new();

        public XmlLogWriter()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _folder = Path.Combine(appData, "EasyLog", "Logs");
            Directory.CreateDirectory(_folder);
            _fileName = $"log-{DateTime.Now:yyyy-MM-dd}.xml";
            _path = Path.Combine(_folder, _fileName);
        }

        public void Write(Object entry)
        {
            lock (_fileLock)
            {
                XDocument doc;

                if (!Directory.Exists(_folder))
                {
                    Directory.CreateDirectory(_folder);
                }

                if (File.Exists(_path))
                {
                    using var readStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    doc = XDocument.Load(readStream);
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

                using var writeStream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                doc.Save(writeStream);
            }
        }
    }
}