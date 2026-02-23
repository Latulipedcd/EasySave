using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EasySave.Application;
using EasySave.Presentation.ViewModels;

namespace GUI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var jobManagementService = ServiceFactory.CreateJobManagementService();
            var languageService = ServiceFactory.GetLanguageService();
            var userConfigService = ServiceFactory.GetUserConfigService();
            var jobRepository = ServiceFactory.GetBackupJobRepository();
            var jobStateReader = ServiceFactory.GetJobStateReader();

            var appViewModel = new BackupAppViewModel(languageService, userConfigService, jobRepository, jobManagementService, jobStateReader);
            desktop.MainWindow = new MainWindow(new MainWindowViewModel(appViewModel));
        }

        base.OnFrameworkInitializationCompleted();
    }
}