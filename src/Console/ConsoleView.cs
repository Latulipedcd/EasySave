using EasySave.Application.ViewModels;

namespace EasySave.ConsoleApp
{
    public class ConsoleView
    {
        private readonly MainViewModel _vm;

        public ConsoleView()
        {
            _vm = new MainViewModel();
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
                        DisplayJobs();
                        break;
                    case "2":
                        CreateJob();
                        break;
                    case "3":
                        ExecuteJobs();
                        break;
                    case "4":
                        UpdateJob();
                        break;
                    case "5":
                        DeleteJob();
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

        private void DisplayJobs()
        {
            Console.Clear();
            Console.WriteLine(_vm.GetText("JobListTitle"));
            Console.WriteLine();

            var jobs = _vm.GetBackupJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine(_vm.GetText("NoJobsFound"));
                Pause();
                return;
            }

            int index = 1;
            foreach (var job in jobs)
            {
                Console.WriteLine($"{index}. {job.Name}");
                Console.WriteLine($"   Source : {job.SourceDirectory}");
                Console.WriteLine($"   Target : {job.TargetDirectory}");
                Console.WriteLine($"   Type   : {job.Type}");
                Console.WriteLine();
                index++;
            }

            Pause();
        }


        private void CreateJob()
        {
            Console.Clear();

            Console.WriteLine(_vm.GetText("AskJobName"));
            var jobTitle = Console.ReadLine();

            Console.WriteLine(_vm.GetText("AskSrcPath"));
            var jobSrcPath = Console.ReadLine();

            Console.WriteLine(_vm.GetText("AskTargetPath"));
            var jobDestPath = Console.ReadLine();

            Console.WriteLine(_vm.GetText("AskJobType"));
            var jobTypeInput = Console.ReadLine();

            if (!int.TryParse(jobTypeInput, out int jobType))
            {
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
                Pause();
                return;
            }

            bool success = _vm.CreateBackupJob(
                jobTitle!,
                jobSrcPath!,
                jobDestPath!,
                jobType,
                out string resultMessage
            );

            Console.WriteLine(resultMessage);
            Pause();
        }

        private void DeleteJob()
        {
            Console.Clear();

            Console.WriteLine(_vm.GetText("AskJobNameToDelete"));
            var jobName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(jobName))
            {
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
                Pause();
                return;
            }

            bool success = _vm.DeleteBackupJob(
                jobName,
                out string resultMessage
            );

            Console.WriteLine(resultMessage);
            Pause();
        }

        private void UpdateJob()
        {
            Console.Clear();

            Console.WriteLine(_vm.GetText("AskJobNameToUpdate"));
            var jobName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(jobName))
            {
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
                Pause();
                return;
            }

            if (!_vm.BackupJobExists(jobName))
            {
                Console.WriteLine(_vm.GetText("ErrorJobNotFound"));
                Pause();
                return;
            }

            Console.WriteLine(_vm.GetText("AskSrcPath"));
            var newSrcPath = Console.ReadLine();

            Console.WriteLine(_vm.GetText("AskTargetPath"));
            var newTargetPath = Console.ReadLine();

            Console.WriteLine(_vm.GetText("AskJobType"));
            var jobTypeInput = Console.ReadLine();

            if (!int.TryParse(jobTypeInput, out int jobType))
            {
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
                Pause();
                return;
            }

            bool success = _vm.UpdateBackupJob(
                jobName,
                newSrcPath!,
                newTargetPath!,
                jobType,
                out string resultMessage
            );

            Console.WriteLine(resultMessage);
            Pause();
        }


        private void ExecuteJobs()
        {
            Console.Clear();

            Console.WriteLine(_vm.GetText("AskJobsToExecute"));
            Console.WriteLine(_vm.GetText("ExecuteHelp"));
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
                Pause();
                return;
            }

            bool success = _vm.ExecuteBackupJobs(
                input,
                out var results,
                out string errorMessage
            );

            if (!success)
            {
                Console.WriteLine(errorMessage);
                Pause();
                return;
            }

            Console.WriteLine();

            foreach (var state in results)
            {
                Console.WriteLine($"{state.Job.Name} : {state.Status}");
            }

            Pause();
        }
    }
    }
