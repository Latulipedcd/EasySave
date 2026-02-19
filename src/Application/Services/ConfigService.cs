using Core.Interfaces;
using Log.Enums;

namespace EasySave.Application.Services;

/// <summary>
/// Application-layer orchestration service for user configuration management.
/// Provides convenient methods with validation and type conversion.
/// </summary>
public class ConfigService
{
    private readonly IUserConfigService _userConfigService;

    public ConfigService(IUserConfigService userConfigService)
    {
        _userConfigService = userConfigService ?? throw new ArgumentNullException(nameof(userConfigService));
    }

    /// <summary>
    /// Changes the log format from a string input ("Json" or "Xml").
    /// </summary>
    public bool ChangeLogFormat(string format)
    {
        if (format == "Json")
        {
            _userConfigService.SaveLogFormat(LogFormat.Json);
            return true;
        }
        if (format == "Xml")
        {
            _userConfigService.SaveLogFormat(LogFormat.Xml);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the saved log format, defaulting to Json if not set.
    /// </summary>
    public LogFormat GetSavedLogFormat()
        => _userConfigService.LoadLogFormat() ?? LogFormat.Json;

    /// <summary>
    /// Changes the business software to monitor.
    /// </summary>
    public bool ChangeBusinessSoftware(string software)
        => _userConfigService.SaveBusinessSoftware(software);

    /// <summary>
    /// Gets the saved business software.
    /// </summary>
    public string? GetSavedBusinessSoftware()
        => _userConfigService.LoadBusinessSoftware();

    /// <summary>
    /// Changes the list of file extensions that CryptoSoft should encrypt.
    /// </summary>
    public bool ChangeCryptoSoftExtensions(List<string> extensions)
        => _userConfigService.SaveCryptoSoftExtensions(extensions);

    /// <summary>
    /// Gets the list of CryptoSoft extensions, defaulting to an empty list if not set.
    /// </summary>
    public List<string> GetCryptoSoftExtensions()
        => _userConfigService.LoadCryptoSoftExtensions() ?? new List<string>();

    /// <summary>
    /// Gets the path to CryptoSoft.exe if it exists.
    /// </summary>
    public string? GetCryptoSoftPath()
    {
        string workDir = AppDomain.CurrentDomain.BaseDirectory;
        string cryptoPath = Path.Combine(workDir, "Resources", "CryptoSoft.exe");
        return File.Exists(cryptoPath) ? cryptoPath : null;
    }
}
