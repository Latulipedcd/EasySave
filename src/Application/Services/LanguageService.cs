using Core.Interfaces;

namespace EasySave.Application.Services;

/// <summary>
/// Application-layer orchestration service for language management.
/// Coordinates between ILanguageService and IUserConfigService.
/// Provides convenience methods like "ChangeLanguage" that load + save in one call.
/// </summary>
public class LanguageService
{
    private readonly ILanguageService _languageService;
    private readonly IUserConfigService _userConfigService;

    public LanguageService(
        ILanguageService languageService,
        IUserConfigService userConfigService)
    {
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _userConfigService = userConfigService ?? throw new ArgumentNullException(nameof(userConfigService));
    }

    /// <summary>
    /// Attempts to load the language saved in user configuration.
    /// </summary>
    public bool TryLoadSavedLanguage()
    {
        string? savedLanguage = _userConfigService.LoadLanguage();
        if (string.IsNullOrWhiteSpace(savedLanguage))
            return false;
        return _languageService.LoadLanguage(savedLanguage);
    }

    /// <summary>
    /// Changes the current language and persists it to user configuration.
    /// </summary>
    public bool ChangeLanguage(string cultureCode)
    {
        bool loaded = _languageService.LoadLanguage(cultureCode);
        if (!loaded)
            return false;
        _userConfigService.SaveLanguage(_languageService.CurrentCultureCode);
        return true;
    }
}
