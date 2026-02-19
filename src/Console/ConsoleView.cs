using Core.Interfaces;
using EasySave.Application;
using EasySave.Application.Services;

namespace EasySave.ConsoleApp
{
    public class ConsoleView
    {

        //Console display
        private readonly ILanguageService _languageService;
        private readonly LanguageService _languageOrchestration;
        private readonly ConfigService _configOrchestration;
        private readonly ConsoleLogic _logic;

        public ConsoleView()
        {
            _languageService = ServiceFactory.GetLanguageService();
            _languageOrchestration = ServiceFactory.CreateLanguageOrchestrationService();
            _configOrchestration = ServiceFactory.CreateConfigOrchestrationService();

            var jobService = ServiceFactory.CreateJobManagementService();
            _logic = new ConsoleLogic(jobService, _languageService);
        }

        public void Start()
        {
            InitializeLanguage();

            bool running = true;
            while (running)
            {
                DisplayMenu();
                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        _logic.DisplayJobs();
                        Pause();
                        break;
                    case "2":
                        _logic.CreateJob();
                        Pause();
                        break;
                    case "3":
                        _logic.ExecuteJobs();
                        Pause();
                        break;
                    case "4":
                        _logic.UpdateJob();
                        Pause();
                        break;
                    case "5":
                        _logic.DeleteJob();
                        Pause();
                        break;
                    case "6":
                        ChangeLanguage();
                        break;
                    case "7":
                        ChooseLogFormat();
                        break;
                    case "8":
                        running = false;
                        break;
                    default:
                        Console.WriteLine(_languageService.GetString("ErrorInvalidOption"));
                        Pause();
                        break;
                }
            }
        }

        private void InitializeLanguage()
        {
            bool hasLoadedSavedLanguage = _languageOrchestration.TryLoadSavedLanguage();

            if (hasLoadedSavedLanguage)
            {
                return;
            }

            RequestLanguageAtStartup();
        }

        private void DisplayMenu()
        {
            Console.Clear();
            Console.WriteLine(_languageService.GetString("MainMenuTitle"));
            Console.WriteLine(_languageService.GetString("MenuOptionList"));
            Console.WriteLine(_languageService.GetString("MenuOptionCreate"));
            Console.WriteLine(_languageService.GetString("MenuOptionExecute"));
            Console.WriteLine(_languageService.GetString("MenuOptionModify"));
            Console.WriteLine(_languageService.GetString("MenuOptionDelete"));
            Console.WriteLine(_languageService.GetString("MenuOptionLang"));
            Console.WriteLine(_languageService.GetString("MenuOptionLog"));
            Console.WriteLine(_languageService.GetString("MenuOptionExit"));
            Console.Write(_languageService.GetString("MenuPrompt"));
        }

        private void RequestLanguageAtStartup()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== EasySave ===");
                Console.WriteLine("Choose your language / Choisissez votre langue");
                Console.WriteLine("1. English");
                Console.WriteLine("2. Français");
                Console.Write("Choice / Choix: ");

                string? choice = Console.ReadLine();
                bool hasChangedLanguage = TryChangeLanguageFromChoice(choice);

                if (hasChangedLanguage)
                {
                    return;
                }

                Console.WriteLine("Invalid option / Option invalide.");
                Pause();
            }
        }

        private void ChangeLanguage()
        {
            Console.WriteLine();
            Console.WriteLine("1. English");
            Console.WriteLine("2. Français");
            Console.Write("Choice: ");

            string? choice = Console.ReadLine();
            bool hasChangedLanguage = TryChangeLanguageFromChoice(choice);

            if (!hasChangedLanguage)
            {
                Console.WriteLine(_languageService.GetString("ErrorInvalidOption"));
                Pause();
            }
        }

        private bool TryChangeLanguageFromChoice(string? choice)
        {
            return choice switch
            {
                "1" => _languageOrchestration.ChangeLanguage("en"),
                "2" => _languageOrchestration.ChangeLanguage("fr"),
                _ => false
            };
        }

        private void ChooseLogFormat()
        {
            Console.WriteLine();
            Console.WriteLine("1. Json (default)");
            Console.WriteLine("2. Xml");
            Console.Write("Choice: ");

            string? choice = Console.ReadLine();
            bool hasChangedLogFormat = TryChangeLogFormatFromChoice(choice);

            if (!hasChangedLogFormat)
            {
                Console.WriteLine(_languageService.GetString("ErrorInvalidOption"));
                Pause();
            }
        }

        private bool TryChangeLogFormatFromChoice(string? choice)
        {
            return choice switch
            {
                "1" => _configOrchestration.ChangeLogFormat("Json"),
                "2" => _configOrchestration.ChangeLogFormat("Xml"),
                _ => false
            };
        }

        private static void Pause()
        {
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }

       
    }
    }
