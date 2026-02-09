using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EasySave.Application.Configuration
{
    public class LanguageManager
    {
        private static LanguageManager? _instance;
        private Dictionary<string, string>? _currentStrings;

        //Path to the Language directory
        private readonly string _langPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Resources",
            "Languages"
        );

        //Language code (en, fr,...) currently in use
        public string CurrentCultureCode { get; private set; } = "en";

        public static LanguageManager GetInstance()
        {
            _instance ??= new LanguageManager();
            return _instance;
        }

        private LanguageManager()
        {
            LoadLanguage("en");
        }

        public bool LoadLanguage(string cultureCode)//Load a language with it's culture code
        {
            string normalizedCultureCode = NormalizeCultureCode(cultureCode);
            string filePath = Path.Combine(_langPath, $"lang.{normalizedCultureCode}.json");

            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                //Change json to key, value
                string jsonContent = File.ReadAllText(filePath);
                Dictionary<string, string>? strings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);

                if (strings == null || strings.Count == 0)
                {
                    return false;
                }

                _currentStrings = strings;
                CurrentCultureCode = normalizedCultureCode;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IReadOnlyList<string> GetSupportedLanguages() //Get the list of language code supported
        {
            if (!Directory.Exists(_langPath))
            {
                return [];
            }

            return Directory.GetFiles(_langPath, "lang.*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Select(fileName => fileName?.Split('.').LastOrDefault())
                .Where(cultureCode => !string.IsNullOrWhiteSpace(cultureCode))
                .Select(cultureCode => cultureCode!.ToLowerInvariant())
                .Distinct()
                .OrderBy(cultureCode => cultureCode)
                .ToList();
        }

        public bool IsSupportedLanguage(string cultureCode)
        {
            string normalizedCultureCode = NormalizeCultureCode(cultureCode);
            return GetSupportedLanguages().Contains(normalizedCultureCode);
        }

        public string GetString(string key) //Get a text in current language
        {
            if (_currentStrings != null && _currentStrings.ContainsKey(key))
            {
                return _currentStrings[key];
            }

            return $"[{key}]";
        }

        private static string NormalizeCultureCode(string cultureCode)
        {
            return string.IsNullOrWhiteSpace(cultureCode)
                ? string.Empty
                : cultureCode.Trim().ToLowerInvariant();
        }
    }
}
