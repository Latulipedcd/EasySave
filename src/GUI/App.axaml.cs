using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Core.Interfaces;
using Core.Services;
using EasySave.Application.Configuration;
using EasySave.Presentation.ViewModels;
using Log.Services;

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
            // Composition Root: create all concrete services
            ILanguageService languageService = LanguageManager.GetInstance();
            IUserConfigService userConfigService = new UserConfigManager();
            IBackupJobRepository jobRepository = new BackupJobRepository(new JobStorage());
            IBackupService backupService = new BackupService(
                LogService.Instance,
                new FileService(),
                new CopyService(),
                new ProgressJsonWriter(),
                new BusinessSoftwareMonitor());

            // Wire up ViewModels
            var mainViewModel = new MainViewModel(languageService, userConfigService, jobRepository, backupService);
            desktop.MainWindow = new MainWindow(new MainWindowViewModel(mainViewModel));
        }

        base.OnFrameworkInitializationCompleted();
    }
}