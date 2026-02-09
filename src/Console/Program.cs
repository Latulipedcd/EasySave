using EasySave.Application.ViewModels;

namespace EasySave.ConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args.Length == 0)
            {
                var view = new ConsoleView();
                view.Start();
                return 0;
            }

            string command = string.Concat(args).Trim();
            if (string.IsNullOrWhiteSpace(command))
            {
                return 0;
            }

            var viewModel = new MainViewModel();
            viewModel.TryLoadSavedLanguage();
            viewModel.ExecuteBackupJobs(command, out _, out _);

            return 0;
        }
    }
}
