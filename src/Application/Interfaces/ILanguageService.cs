using System.Collections.Generic;

namespace EasySave.Application.Interfaces;

public interface ILanguageService
{
    string CurrentCultureCode { get; }
    bool LoadLanguage(string cultureCode);
    string GetString(string key);
    IReadOnlyList<string> GetSupportedLanguages();
}
