using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace GUI;

/// <summary>
/// Classe principale de l'application Avalonia
/// Gère le cycle de vie de l'application et l'initialisation des ressources
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initialise l'application en chargeant les ressources XAML
    /// Appelée automatiquement au démarrage de l'application
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Appelée lorsque l'initialisation du framework est terminée
    /// Configure la fenêtre principale pour les applications desktop classiques
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Création et affichage de la fenêtre principale
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}