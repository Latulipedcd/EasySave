using Avalonia;
using System;

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
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Configure l'application Avalonia avec les paramètres nécessaires
    /// </summary>
    /// <returns>AppBuilder configuré</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()        // Détection automatique de la plateforme (Windows/Linux/macOS)
            .WithInterFont()            // Police Inter par défaut pour une typographie moderne
            .LogToTrace();              // Journalisation des messages de debug vers Trace
}
