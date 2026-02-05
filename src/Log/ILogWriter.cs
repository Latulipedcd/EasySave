using System.Text.Json;
using System.Text.Json.Nodes;

namespace Log
{
    internal interface ILogWriter
    {
        void Write(Object entry);
    }


    class JsonLogWriter : ILogWriter
    {
        public void Write(Object entry)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            string fileName = $"log-{DateTime.Now:yyyy-MM-dd}.json";
            string path = Path.Combine(folder, fileName);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (!File.Exists(path))
            {
                File.WriteAllText(path, "[]");
            }

            var json = File.ReadAllText(path);

            JsonArray array = JsonNode.Parse(json)?.AsArray() ?? new JsonArray();

            JsonNode newEntry = JsonSerializer.SerializeToNode(entry);

            array.Add(newEntry);

            File.WriteAllText(path, array.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
    }
}

