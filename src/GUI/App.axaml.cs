using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Core.Interfaces;
using Core.Services;
using EasySave.Application.Configuration;
using EasySave.Application.Services;
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
            IProgressWriter progressWriter = new ProgressJsonWriter();
            IBackupService backupService = new BackupService(
                LogService.Instance,
                new FileService(),
                new CopyService(),
                progressWriter,
                new BusinessSoftwareMonitor());

            IJobManagementService jobManagementService = new JobManagementService(
                languageService,
                userConfigService,
                jobRepository,
                backupService,
                new BusinessSoftwareMonitor(),
                progressWriter);

            // Wire up ViewModels
            var mainViewModel = new MainViewModel(languageService, userConfigService, jobRepository, jobManagementService);
            desktop.MainWindow = new MainWindow(new MainWindowViewModel(mainViewModel));
        }

        base.OnFrameworkInitializationCompleted();
    }
}