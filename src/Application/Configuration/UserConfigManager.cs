using System;
using System.IO;
using System.Text.Json;

namespace EasySave.Application.Configuration
{

    //Persistant language configuration
    public class UserConfigManager
    {
        private readonly string _configDirectoryPath;
        private readonly string _configFilePath;

        public UserConfigManager()
        {
            _configDirectoryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave",
                "Userdata"
            );

            _configFilePath = Path.Combine(_configDirectoryPath, "userconfig.json");
        }

        public string? LoadLanguage()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    return null;
                }

                string jsonContent = File.ReadAllText(_configFilePath);
                UserConfig? userConfig = JsonSerializer.Deserialize<UserConfig>(
                    jsonContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (string.IsNullOrWhiteSpace(userConfig?.Language))
                {
                    return null;
                }

                return userConfig.Language.Trim().ToLowerInvariant();
            }
            catch
            {
                return null;
            }
        }

        public bool SaveLanguage(string cultureCode)
        {
            if (string.IsNullOrWhiteSpace(cultureCode))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(_configDirectoryPath);

                UserConfig userConfig = new()
                {
                    Language = cultureCode.Trim().ToLowerInvariant()
                };

                string jsonContent = JsonSerializer.Serialize(
                    userConfig,
                    new JsonSerializerOptions { WriteIndented = true }
                );

                File.WriteAllText(_configFilePath, jsonContent);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private sealed class UserConfig
        {
            public string? Language { get; set; }
        }
    }
}
