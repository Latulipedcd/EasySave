using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EasySave.Application.Configuration;
using Log.Enums;

namespace EasySave.Application.ViewModels;

/// <summary>
/// ViewModel responsible for managing application settings (language, log format, business software, crypto extensions)
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly LanguageManager _langManager;
    private readonly UserConfigManager _userConfigManager;
    private SettingItemViewModel? _languageSetting;
    private SettingItemViewModel? _logFormatSetting;
    private SettingItemViewModel? _businessSoftwareSetting;
    private SettingItemViewModel? _cryptoExtensionsSetting;

    /// <summary>
    /// Collection of supported languages (en, fr, etc.)
    /// </summary>
    public ObservableCollection<string> SupportedLanguages { get; }

    /// <summary>
    /// Modular list of setting items displayed in the settings menu
    /// </summary>
    public ObservableCollection<SettingItemViewModel> SettingsItems { get; }

    /// <summary>
    /// Selected language code (en, fr, etc.)
    /// </summary>
    private string _selectedLanguageCode = "en";
    public string SelectedLanguageCode
    {
        get => _selectedLanguageCode;
        set
        {
            if (value == _selectedLanguageCode) return;

            var previous = _selectedLanguageCode;
            _selectedLanguageCode = value;
            OnPropertyChanged();

            if (string.IsNullOrWhiteSpace(value))
                return;

            // Attempt to change language
            var ok = _langManager.LoadLanguage(value);
            if (!ok)
            {
                // Rollback if failure
                _selectedLanguageCode = previous;
                OnPropertyChanged(nameof(SelectedLanguageCode));
                _languageSetting?.SetSelectedValue(_selectedLanguageCode);
                return;
            }

            _userConfigManager.SaveLanguage(_langManager.CurrentCultureCode);
            _languageSetting?.SetSelectedValue(_selectedLanguageCode);
            // Trigger event for UI text refresh
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Selected log format index (0=Json, 1=Xml)
    /// </summary>
    private int _selectedLogFormatIndex;
    public int SelectedLogFormatIndex
    {
        get => _selectedLogFormatIndex;
        set
        {
            if (value == _selectedLogFormatIndex) return;
            if (value != 0 && value != 1) return;

            _selectedLogFormatIndex = value;
            OnPropertyChanged();

            var format = value == 1 ? LogFormat.Xml : LogFormat.Json;
            _userConfigManager.SaveLogFormat(format);
            _logFormatSetting?.SetSelectedValue(value == 1 ? "Xml" : "Json");
        }
    }

    /// <summary>
    /// Business software name to block during backups
    /// </summary>
    private string _businessSoftware = string.Empty;
    public string BusinessSoftware
    {
        get => _businessSoftware;
        set
        {
            if (value == _businessSoftware) return;

            _businessSoftware = value ?? string.Empty;
            OnPropertyChanged();

            _userConfigManager.SaveBusinessSoftware(_businessSoftware);
            _businessSoftwareSetting?.SetTextValue(_businessSoftware);
        }
    }

    /// <summary>
    /// File extensions to encrypt (comma-separated)
    /// </summary>
    private string _cryptoExtensions = string.Empty;
    public string CryptoExtensions
    {
        get => _cryptoExtensions;
        set
        {
            if (value == _cryptoExtensions) return;

            _cryptoExtensions = value ?? string.Empty;
            OnPropertyChanged();

            // Parse comma-separated extensions and save to config
            var extensionsList = _cryptoExtensions
                .Split(',')
                .Select(ext => ext.Trim())
                .Where(ext => !string.IsNullOrWhiteSpace(ext))
                .ToList();

            _userConfigManager.SaveCryptoSoftExtensions(extensionsList);
            _cryptoExtensionsSetting?.SetTextValue(_cryptoExtensions);
        }
    }

    /// <summary>
    /// Event raised when language changes (for UI text refresh)
    /// </summary>
    public event EventHandler? LanguageChanged;

    public SettingsViewModel(LanguageManager langManager, UserConfigManager userConfigManager)
    {
        _langManager = langManager;
        _userConfigManager = userConfigManager;

        // Load saved settings
        var savedLanguage = _userConfigManager.LoadLanguage();
        if (!string.IsNullOrWhiteSpace(savedLanguage))
        {
            _langManager.LoadLanguage(savedLanguage);
        }

        SupportedLanguages = new ObservableCollection<string>(_langManager.GetSupportedLanguages());
        _selectedLanguageCode = _langManager.CurrentCultureCode;
        _selectedLogFormatIndex = _userConfigManager.LoadLogFormat() == LogFormat.Xml ? 1 : 0;
        _businessSoftware = _userConfigManager.LoadBusinessSoftware() ?? string.Empty;
        _cryptoExtensions = string.Join(", ", _userConfigManager.LoadCryptoSoftExtensions() ?? new List<string>());

        SettingsItems = new ObservableCollection<SettingItemViewModel>();
    }

    /// <summary>
    /// Builds settings menu items with localized labels
    /// </summary>
    public void BuildSettingsMenuItems(
        string languageLabel,
        string logFormatLabel,
        string logFormatJsonLabel,
        string logFormatXmlLabel,
        string businessSoftwareLabel,
        string cryptoExtensionsLabel)
    {
        SettingsItems.Clear();

        _languageSetting = new SettingItemViewModel(
            languageLabel,
            SupportedLanguages.Select(code => new SettingOptionViewModel(code, code.ToUpperInvariant())),
            SelectedLanguageCode,
            value => SelectedLanguageCode = value);

        _logFormatSetting = new SettingItemViewModel(
            logFormatLabel,
            GetLogFormatOptions(logFormatJsonLabel, logFormatXmlLabel),
            SelectedLogFormatIndex == 1 ? "Xml" : "Json",
            value => SelectedLogFormatIndex =
                string.Equals(value, "Xml", StringComparison.OrdinalIgnoreCase) ? 1 : 0);

        _businessSoftwareSetting = new SettingItemViewModel(
            businessSoftwareLabel,
            _businessSoftware,
            value => BusinessSoftware = value);

        _cryptoExtensionsSetting = new SettingItemViewModel(
            cryptoExtensionsLabel,
            _cryptoExtensions,
            value => CryptoExtensions = value);

        SettingsItems.Add(_languageSetting);
        SettingsItems.Add(_logFormatSetting);
        SettingsItems.Add(_businessSoftwareSetting);
        SettingsItems.Add(_cryptoExtensionsSetting);
    }

    /// <summary>
    /// Refreshes settings menu items with new localized labels
    /// </summary>
    public void RefreshSettingsMenuItems(
        string languageLabel,
        string logFormatLabel,
        string logFormatJsonLabel,
        string logFormatXmlLabel,
        string businessSoftwareLabel,
        string cryptoExtensionsLabel)
    {
        if (_languageSetting != null)
        {
            _languageSetting.UpdateLabel(languageLabel);
            _languageSetting.SetSelectedValue(SelectedLanguageCode);
        }

        if (_logFormatSetting != null)
        {
            _logFormatSetting.UpdateLabel(logFormatLabel);
            _logFormatSetting.ReplaceOptions(
                GetLogFormatOptions(logFormatJsonLabel, logFormatXmlLabel),
                SelectedLogFormatIndex == 1 ? "Xml" : "Json");
        }

        if (_businessSoftwareSetting != null)
        {
            _businessSoftwareSetting.UpdateLabel(businessSoftwareLabel);
            _businessSoftwareSetting.SetTextValue(_businessSoftware);
        }

        if (_cryptoExtensionsSetting != null)
        {
            _cryptoExtensionsSetting.UpdateLabel(cryptoExtensionsLabel);
            _cryptoExtensionsSetting.SetTextValue(_cryptoExtensions);
        }

        OnPropertyChanged(nameof(SettingsItems));
    }

    public string CurrentLanguageCode => _langManager.CurrentCultureCode;

    public string GetText(string key) => _langManager.GetString(key);

    public LogFormat GetLogFormat() => _userConfigManager.LoadLogFormat() ?? LogFormat.Json;

    public string? GetBusinessSoftware() => _userConfigManager.LoadBusinessSoftware();

    public List<string> GetCryptoExtensions() => _userConfigManager.LoadCryptoSoftExtensions() ?? new List<string>();

    private IEnumerable<SettingOptionViewModel> GetLogFormatOptions(string jsonLabel, string xmlLabel)
    {
        yield return new SettingOptionViewModel("Json", jsonLabel);
        yield return new SettingOptionViewModel("Xml", xmlLabel);
    }
}
