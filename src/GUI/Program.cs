using Avalonia;
using EasySave.Application;
using System;
using System.Diagnostics;

namespace GUI;

/// <summary>
/// Point d'entrée de l'application EasySave GUI
/// Configure et démarre l'application Avalonia
/// </summary>
class Program
{
    /// <summary>
    /// Point d'entrée principal de l'application
    /// [STAThread] requis pour Windows pour la compatibilité COM et les dialogues système
    /// </summary>
    /// <param name="args">Arguments de ligne de commande</param>
    [STAThread]
    public static int Main(string[] args)
    {
        TryInstallCliCommand();

        if (args.Length > 0)
        {
            return ExecuteFromCommandLine(args);
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    /// <summary>
    /// Configure l'application Avalonia avec les paramètres nécessaires
    /// </summary>
    /// <returns>AppBuilder configuré</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()        // Détection automatique de la plateforme (Windows/Linux/macOS)
            .WithInterFont()            // Police Inter par défaut pour une typographie moderne
            .LogToTrace();              // Journalisation des messages de debug vers Trace

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

    private static void TryInstallCliCommand()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            string scriptPath = "scripts\\install-easysave-cli.cmd";

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{scriptPath}\"\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch
        {
            // Do not block app startup if installation fails.
        }
    }
}
