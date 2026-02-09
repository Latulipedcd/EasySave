using EasySave.Application.ViewModels;

namespace EasySave.ConsoleApp
{
    public class ConsoleView
    {

        //Console display
        private readonly MainViewModel _vm;
        private readonly ConsoleLogic _logic;

        public ConsoleView()
        {
            _vm = new MainViewModel();
            _logic = new ConsoleLogic();
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
                        running = false;
                        break;
                    default:
                        Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
                        Pause();
                        break;
                }
            }
        }

        private void InitializeLanguage()
        {
            bool hasLoadedSavedLanguage = _vm.TryLoadSavedLanguage();

            if (hasLoadedSavedLanguage)
            {
                return;
            }

            RequestLanguageAtStartup();
        }

        private void DisplayMenu()
        {
            Console.Clear();
            Console.WriteLine(_vm.GetText("MainMenuTitle"));
            Console.WriteLine(_vm.GetText("MenuOptionList"));
            Console.WriteLine(_vm.GetText("MenuOptionCreate"));
            Console.WriteLine(_vm.GetText("MenuOptionExecute"));
            Console.WriteLine(_vm.GetText("MenuOptionModify"));
            Console.WriteLine(_vm.GetText("MenuOptionDelete"));
            Console.WriteLine(_vm.GetText("MenuOptionLang"));
            Console.WriteLine(_vm.GetText("MenuOptionExit"));
            Console.Write(_vm.GetText("MenuPrompt"));
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
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
                Pause();
            }
        }

        private bool TryChangeLanguageFromChoice(string? choice)
        {
            return choice switch
            {
                "1" => _vm.ChangeLanguage("en"),
                "2" => _vm.ChangeLanguage("fr"),
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
