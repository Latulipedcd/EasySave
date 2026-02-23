using EasySave.Application.Interfaces;
using Log.Enums;
using Core.Enums;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySave.Application.Configuration
{
    public class UserConfigManager : IUserConfigService
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

        public bool SaveCryptoSoftExtensions(List<string> extensions)
        {
            try
            {
                Directory.CreateDirectory(_configDirectoryPath);

                var userConfig = LoadConfig(); // on charge l'existant
                userConfig.CryptoSoftExtensions = extensions ?? new List<string>();

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

        public List<string>? LoadPriorityExtensions()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return null;

                string json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<UserConfig>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return config?.PriorityExtensions;
            }
            catch { return null; }
        }

        public bool SavePriorityExtensions(List<string> extensions)
        {
            try
            {
                Directory.CreateDirectory(_configDirectoryPath);
                var userConfig = LoadConfig();
                userConfig.PriorityExtensions = extensions ?? new List<string>();
                File.WriteAllText(_configFilePath, JsonSerializer.Serialize(
                    userConfig, new JsonSerializerOptions { WriteIndented = true }));
                return true;
            }
            catch { return false; }
        }

        public long LoadMaxParallelFileSizeKb()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return 0;

                string json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<UserConfig>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return config?.MaxParallelFileSizeKb ?? 0;
            }
            catch { return 0; }
        }

        public bool SaveMaxParallelFileSizeKb(long sizeKb)
        {
            try
            {
                Directory.CreateDirectory(_configDirectoryPath);
                var userConfig = LoadConfig();
                userConfig.MaxParallelFileSizeKb = sizeKb < 0 ? 0 : sizeKb;
                File.WriteAllText(_configFilePath, JsonSerializer.Serialize(
                    userConfig, new JsonSerializerOptions { WriteIndented = true }));
                return true;
            }
            catch { return false; }
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

        public bool SaveStorageMode(LogStorageMode mode)
        {

            try
            {
                Directory.CreateDirectory(_configDirectoryPath);

                var userConfig = LoadConfig(); // on charge l’existant
                userConfig.StorageMode = mode;

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

        public LogStorageMode? LoadStorageMode()
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

                return config?.StorageMode;
            }
            catch
            {
                return null;
            }
        }

        private sealed class UserConfig
        {
            public string? Language { get; set; }
            public LogFormat? SavedLogFormat { get; set; }
            public string? BusinessSoftware { get; set; }
            public List<string> CryptoSoftExtensions { get; set; } = new();

            /// <summary>File extensions treated as priority during parallel execution.</summary>
            public List<string> PriorityExtensions { get; set; } = new();

            /// <summary>
            /// Max file size (KB) that can be transferred in parallel.
            /// 0 = large-file bandwidth rule disabled.
            /// </summary>
            public long MaxParallelFileSizeKb { get; set; } = 0;

            public LogStorageMode? StorageMode { get; set; }
        }
    }
}
