namespace EasySave.Application.Interfaces;

/// <summary>
/// Service responsible for managing application localization and retrieving translated strings.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Gets the culture code of the currently loaded and active language (e.g., "en-US", "fr-FR").
    /// </summary>
    string CurrentCultureCode { get; }

    /// <summary>
    /// Attempts to load the localization resources for the specified culture code.
    /// </summary>
    /// <param name="cultureCode">The culture code of the language to load (e.g., "en-US").</param>
    /// <returns><c>true</c> if the language was successfully loaded;
    /// otherwise, <c>false</c>.</returns>
    bool LoadLanguage(string cultureCode);

    /// <summary>
    /// Retrieves the localized string value associated with the specified translation key.
    /// </summary>
    /// <param name="key">The unique identifier for the localized string.</param>
    /// <returns>The localized string if found; otherwise, 
    /// a fallback value or the key itself depending on the implementation.</returns>
    string GetString(string key);

    /// <summary>
    /// Retrieves a read-only list of all culture codes or language names currently supported by the application.
    /// </summary>
    /// <returns>An <see cref="IReadOnlyList{String}"/> containing 
    /// the supported language codes.</returns>
    IReadOnlyList<string> GetSupportedLanguages();
}
