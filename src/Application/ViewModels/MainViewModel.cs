using EasySave.Application.Configuration;

namespace EasySave.Application.ViewModels
{
    public class MainViewModel
    {
        private LanguageManager _langManager;

        public MainViewModel()
        {
            _langManager = LanguageManager.GetInstance();
        }

        // Method to switch language from the View
        public void ChangeLanguage(string cultureCode)
        {
            _langManager.LoadLanguage(cultureCode);
        }

        // Property to access strings (Simulating Binding)
        // In WPF, this would use an Indexer or a specific Binding object.
        // In Console, a simple method suffices.
        public string GetText(string key)
        {
            return _langManager.GetString(key);
        }
    }
}