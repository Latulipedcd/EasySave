namespace EasySave.ConsoleApp
{
    class Program
    {
        // static MainViewModel _viewModel; // The binding context

        static void Main(string[] args)
        {
            // _viewModel = new MainViewModel(); // Initialize ViewModel

            Console.OutputEncoding = System.Text.Encoding.Unicode;
            if (args.Length > 0)
            {
                // --- COMMAND LINE MODE ---
                // Used when arguments are passed (e.g., "EasySave.exe 1-3")
                HandleCommandLineArgs(args);
            }
            else
            {
                // --- INTERACTIVE MODE ---
                // Used when the user opens the executable directly
                var view = new ConsoleView();
                view.Start();
            }
        }

        static void HandleCommandLineArgs(string[] args)
        {
            string command = args[0];

            // TODO: Pass this logic to the ViewModel to parse ranges (1-3) or lists (1;3)
            // Example: _viewModel.ExecuteJobsByCommandLine(command);

            Console.WriteLine($"[View Log]: Requesting execution for arguments: {command}");
        }
    }
}