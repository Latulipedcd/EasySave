using EasySave.Application.ViewModels;
using System.Reflection;

namespace EasySave.ConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            TryInstallCliCommand();

            if (args.Length == 0)
            {
                var view = new ConsoleView();
                view.Start();
                return 0;
            }

            return ExecuteFromCommandLine(args);
        }

        private static int ExecuteFromCommandLine(string[] args)
        {
            string command = NormalizeCommandInput(args);
            var viewModel = new MainViewModel();
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

        private static string NormalizeCommandInput(string[] args)
        {
            var parts = new List<string>();

            foreach (string arg in args)
            {
                string trimmed = arg.Trim();
                if (trimmed.Length > 0)
                {
                    parts.Add(trimmed);
                }
            }

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            if (parts.Count == 1)
            {
                return parts[0].Replace(',', ';');
            }

            // Supports `EasySave 1 3` in shells where `;` is treated as a command separator.
            return string.Join(';', parts);
        }

        private static void TryInstallCliCommand()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                // Small bootstrap install:
                // 1) create EasySave.cmd in a stable user folder
                // 2) ensure this folder exists in user PATH
                string installDir = GetInstallDirectory();
                Directory.CreateDirectory(installDir);
                WriteCommandShim(installDir);
                AddInstallDirToUserPathIfMissing(installDir);
            }
            catch
            {
                // Keep startup behavior unchanged even if CLI installation fails.
            }
        }

        private static string GetInstallDirectory()
        {
            string? configuredPath = Environment.GetEnvironmentVariable("EASYSAVE_INSTALL_DIR");
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return Environment.ExpandEnvironmentVariables(configuredPath);
            }

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "EasySave", "bin");
        }

        private static void WriteCommandShim(string installDir)
        {
            string shimPath = Path.Combine(installDir, "EasySave.cmd");
            string shimContent = $"@echo off{Environment.NewLine}{BuildCommandTarget()} %*{Environment.NewLine}";
            File.WriteAllText(shimPath, shimContent);
        }

        private static string BuildCommandTarget()
        {
            // Preferred: call the apphost executable directly.
            string exePath = Path.Combine(AppContext.BaseDirectory, "EasySave.exe");
            if (File.Exists(exePath))
            {
                return $"\"{exePath}\"";
            }

            string? entryAssemblyPath = Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrWhiteSpace(entryAssemblyPath))
            {
                return $"dotnet \"{entryAssemblyPath}\"";
            }

            string dllPath = Path.Combine(AppContext.BaseDirectory, "EasySave.dll");
            return $"dotnet \"{dllPath}\"";
        }

        private static void AddInstallDirToUserPathIfMissing(string installDir)
        {
            string normalizedInstallDir = NormalizeForComparison(installDir);
            string? userPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
            string[] pathParts = (userPath ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawPart in pathParts)
            {
                if (NormalizeForComparison(rawPart) == normalizedInstallDir)
                {
                    return;
                }
            }

            string newUserPath = string.IsNullOrWhiteSpace(userPath)
                ? installDir
                : $"{userPath.TrimEnd(';')};{installDir}";

            Environment.SetEnvironmentVariable("Path", newUserPath, EnvironmentVariableTarget.User);
        }

        private static string NormalizeForComparison(string path)
        {
            return Path.GetFullPath(path)
                .Trim()
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToUpperInvariant();
        }
    }
}

