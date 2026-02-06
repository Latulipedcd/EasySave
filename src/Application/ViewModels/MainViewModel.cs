using EasySave.Application.Configuration;
using System.Collections.Generic;

namespace EasySave.Application.ViewModels
{
    public class MainViewModel
    {
        private readonly LanguageManager _langManager;
        private readonly UserConfigManager _userConfigManager;

        public MainViewModel()
        {
            _langManager = LanguageManager.GetInstance();
            _userConfigManager = new UserConfigManager();
        }

        public bool TryLoadSavedLanguage()
        {
            string? savedLanguage = _userConfigManager.LoadLanguage();

            if (string.IsNullOrWhiteSpace(savedLanguage))
            {
                return false;
            }

            return _langManager.LoadLanguage(savedLanguage);
        }

        public bool ChangeLanguage(string cultureCode)
        {
            bool isLanguageLoaded = _langManager.LoadLanguage(cultureCode);
            if (!isLanguageLoaded)
            {
                return false;
            }

            _userConfigManager.SaveLanguage(_langManager.CurrentCultureCode);
            return true;
        }

        public IReadOnlyList<string> GetSupportedLanguages()
        {
            return _langManager.GetSupportedLanguages();
        }

        public string GetText(string key)
        {
            return _langManager.GetString(key);
        }
    }
}
