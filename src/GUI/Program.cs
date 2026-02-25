using Avalonia;
using EasySave.Application;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GUI;

/// <summary>
/// Point d'entrée de l'application EasySave GUI
/// Configure et démarre l'application Avalonia
/// </summary>
class Program
{
    private const int ExitSuccess = 0;
    private const int ExitFailure = 1;

    /// <summary>
    /// Point d'entrée principal de l'application
    /// [STAThread] requis pour Windows pour la compatibilité COM et les dialogues système
    /// </summary>
    /// <param name="args">Arguments de ligne de commande</param>
    [STAThread]
    public static int Main(string[] args)
    {
        TryInstallCliCommand();

        if (TryBuildCommandInput(args, out string commandInput))
        {
            return ExecuteFromCommandLine(commandInput);
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return ExitSuccess;
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

    private static bool TryBuildCommandInput(string[] args, out string commandInput)
    {
        commandInput = string.Empty;

        if (args is null || args.Length == 0)
        {
            return false;
        }

        var jobTokens = new List<string>();
        foreach (var rawArg in args)
        {
            if (string.IsNullOrWhiteSpace(rawArg))
            {
                continue;
            }

            jobTokens.Add(rawArg.Trim());
        }

        if (jobTokens.Count == 0)
        {
            return false;
        }

        // `EasySave 1 3` becomes `1;3`
        commandInput = string.Join(';', jobTokens);
        return true;
    }

    private static int ExecuteFromCommandLine(string commandInput)
    {
        try
        {
            var languageServiceManager = ServiceFactory.CreateLanguageService();
            var jobManagementService = ServiceFactory.CreateJobManagementService();
            languageServiceManager.TryLoadSavedLanguage();

            Console.WriteLine($"EasySave: running jobs {commandInput}...");

            bool success = jobManagementService.ExecuteBackupJobs(commandInput, out _, out string errorMessage);
            if (!success)
            {
                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    Console.WriteLine("EasySave: backup failed.");
                }
                else
                {
                    Console.WriteLine($"EasySave: backup failed - {errorMessage}");
                }

                return ExitFailure;
            }

            Console.WriteLine("EasySave: backup completed.");
            return ExitSuccess;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EasySave: backup failed ({ex.Message}).");
            return ExitFailure;
        }
    }

    private static void TryInstallCliCommand()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (IsRunningFromCliInstallFolder())
        {
            return;
        }

        try
        {
            string scriptPath = Path.Combine(Environment.CurrentDirectory, "scripts", "install-easysave-cli.cmd");
            if (!File.Exists(scriptPath))
            {
                return;
            }

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

    private static bool IsRunningFromCliInstallFolder()
    {
        string installFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EasySave",
            "bin");

        string currentBaseFolder = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedInstallFolder = Path.GetFullPath(installFolder)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return string.Equals(currentBaseFolder, normalizedInstallFolder, StringComparison.OrdinalIgnoreCase);
    }

}
