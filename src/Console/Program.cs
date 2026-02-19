using Core.Interfaces;
using EasySave.Application;
using EasySave.Application.Services;

namespace EasySave.ConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            if (args.Length == 0)
            {
                var consoleView = new ConsoleView();
                consoleView.Start();
                return 0;
            }

            return ExecuteFromCommandLine(args);
        }

        private static int ExecuteFromCommandLine(string[] args)
        {
            if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
            {
                var langService = ServiceFactory.GetLanguageService();
                var langOrchestration = ServiceFactory.CreateLanguageOrchestrationService();
                langOrchestration.TryLoadSavedLanguage();
                Console.WriteLine(langService.GetString("ErrorInvalidOption"));
                return 1;
            }

            string command = args[0];
            var languageService = ServiceFactory.GetLanguageService();
            var languageOrchestrationService = ServiceFactory.CreateLanguageOrchestrationService();
            var jobManagementService = ServiceFactory.CreateJobManagementService();

            languageOrchestrationService.TryLoadSavedLanguage();

            if (string.IsNullOrWhiteSpace(command))
            {
                Console.WriteLine(languageService.GetString("ErrorInvalidOption"));
                return 1;
            }

            bool success = jobManagementService.ExecuteBackupJobs(command, out var results, out string errorMessage);
            if (!success)
            {
                Console.WriteLine(errorMessage);
                return 1;
            }

            foreach (var state in results)
            {
                Console.WriteLine($"{state.Job.Name} : {state.Status}");
            }

            return 0;
        }
    }
}

