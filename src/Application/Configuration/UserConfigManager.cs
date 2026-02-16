using Log.Enums;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                "userdata"
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

                var userConfig = LoadConfig(); // on charge l’existant
                userConfig.Language = cultureCode.Trim().ToLowerInvariant();

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

        public LogFormat? LoadLogFormat()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return null;

                string json = File.ReadAllText(_configFilePath);

                var config = JsonSerializer.Deserialize<UserConfig>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() }
                    });

                return config?.SavedLogFormat;
            }
            catch
            {
                return null;
            }
        }


        public bool SaveLogFormat(LogFormat format)
        {
            
            try
            {
                Directory.CreateDirectory(_configDirectoryPath);

                var userConfig = LoadConfig(); // on charge l’existant
                userConfig.SavedLogFormat = format;

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

        public string? LoadBusinessSoftware()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return null;

                string json = File.ReadAllText(_configFilePath);

                var config = JsonSerializer.Deserialize<UserConfig>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() }
                    });

                return config?.BusinessSoftware;
            }
            catch
            {
                return null;
            }
        }

        public bool SaveBusinessSoftware(string software)
        {

            try
            {
                Directory.CreateDirectory(_configDirectoryPath);

                var userConfig = LoadConfig(); // on charge l’existant
                userConfig.BusinessSoftware = software;

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

        public List<string>? LoadCryptoSoftExtensions()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return null;

                string json = File.ReadAllText(_configFilePath);

                var config = JsonSerializer.Deserialize<UserConfig>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() }
                    });

                return config?.CryptoSoftExtensions;
            }
            catch
            {
                return null;
            }
        }

        public bool AddCryptoSoftExtension(string extension)
        {

            try
            {
                Directory.CreateDirectory(_configDirectoryPath);

                var userConfig = LoadConfig(); // on charge l’existant
                if (!userConfig.CryptoSoftExtensions.Contains(extension))
                {
                    userConfig.CryptoSoftExtensions.Add(extension);
                }

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

        public bool RemoveCryptoSoftExtension(string extension)
        {

            try
            {
                Directory.CreateDirectory(_configDirectoryPath);

                var userConfig = LoadConfig(); // on charge l’existant
                if (userConfig.CryptoSoftExtensions.Contains(extension))
                {
                    userConfig.CryptoSoftExtensions.Remove(extension);
                }

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

        private UserConfig LoadConfig()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return new UserConfig();

                string json = File.ReadAllText(_configFilePath);
                return JsonSerializer.Deserialize<UserConfig>(json) ?? new UserConfig();
            }
            catch
            {
                return new UserConfig();
            }
        }

        private sealed class UserConfig
        {
            public string? Language { get; set; }
            public LogFormat? SavedLogFormat { get; set; }
            public string? BusinessSoftware { get; set; }

            public List<string> CryptoSoftExtensions { get; set; } = new();
        }
    }
}
