using EasySave.Application;

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
                var vm = new BackupController();
                vm.TryLoadSavedLanguage();
                Console.WriteLine(vm.GetText("ErrorInvalidOption"));
                return 1;
            }

            string command = args[0];
            var viewModel = new BackupController();
            viewModel.TryLoadSavedLanguage();

            if (string.IsNullOrWhiteSpace(command))
            {
                Console.WriteLine(viewModel.GetText("ErrorInvalidOption"));
                return 1;
            }

            bool success = viewModel.ExecuteBackupJobs(command, out var results, out string errorMessage);
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

