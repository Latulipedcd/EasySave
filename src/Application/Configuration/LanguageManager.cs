using System.Collections.Generic;
using System.IO;
using System.Text.Json; // Requires System.Text.Json NuGet or .NET Core

namespace EasySave.Application.Configuration
{
    public class LanguageManager
    {
        private static LanguageManager _instance;
        private Dictionary<string, string> _currentStrings;

        private readonly string _langPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, // The folder where .exe is running
            "Resources",
            "Languages"
        );
        public static LanguageManager GetInstance()
        {
            if (_instance == null) _instance = new LanguageManager();
            return _instance;
        }

        private LanguageManager()
        {
            // Default language
            LoadLanguage("en");
        }

        public void LoadLanguage(string cultureCode)
        {
            string filePath = Path.Combine(_langPath, $"lang.{cultureCode}.json");

            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                _currentStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
            }
            else
            {
                // Fallback or Error handling
                // Ideally, load a hardcoded fallback here so the app doesn't crash
            }
        }

        // The method the ViewModel calls to get text
        public string GetString(string key)
        {
            if (_currentStrings != null && _currentStrings.ContainsKey(key))
            {
                return _currentStrings[key];
            }
            return $"[{key}]"; // Returns [KeyName] if translation is missing (Debug friendly)
        }
    }
}