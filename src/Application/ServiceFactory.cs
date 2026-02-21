using Core.Interfaces;
using Core.Services;
using EasySave.Application.Configuration;
using EasySave.Application.Services;
using Log.Services;

namespace EasySave.Application;

/// <summary>
/// Factory for creating application services with all their dependencies wired up.
/// This centralizes the "new" keywords and provides a clean way for UI layers to get services.
/// In a real DI container (like Microsoft.Extensions.DependencyInjection), this would be replaced by container registration.
/// </summary>
public static class ServiceFactory
{
    private static ILanguageService? _languageServiceInstance;
    private static IUserConfigService? _userConfigServiceInstance;
    private static IBackupJobRepository? _backupJobRepositoryInstance;
    private static IBackupService? _backupServiceInstance;
    private static IProgressWriter? _progressWriterInstance;

    /// <summary>
    /// Gets or creates the singleton ILanguageService instance.
    /// </summary>
    public static ILanguageService GetLanguageService()
    {
        return _languageServiceInstance ??= LanguageManager.GetInstance();
    }

    /// <summary>
    /// Gets or creates the singleton IUserConfigService instance.
    /// </summary>
    public static IUserConfigService GetUserConfigService()
    {
        return _userConfigServiceInstance ??= new UserConfigManager();
    }

    /// <summary>
    /// Gets or creates the singleton IBackupJobRepository instance.
    /// </summary>
    public static IBackupJobRepository GetBackupJobRepository()
    {
        return _backupJobRepositoryInstance ??= new BackupJobRepository(new JobStorage());
    }

    /// <summary>
    /// Gets or creates the singleton IProgressWriter instance.
    /// </summary>
    public static IProgressWriter GetProgressWriter()
    {
        return _progressWriterInstance ??= new ProgressJsonWriter();
    }

    /// <summary>
    /// Gets or creates the singleton IBackupService instance.
    /// </summary>
    public static IBackupService GetBackupService()
    {
        return _backupServiceInstance ??= new Core.Services.BackupService(
            LogService.Instance,
            new FileService(),
            new CopyService(),
            GetProgressWriter(),
            new BusinessSoftwareMonitor());
    }

    /// <summary>
    /// Creates a new instance of IJobManagementService with all dependencies.
    /// </summary>
    public static IJobManagementService CreateJobManagementService()
    {
        return new JobManagementService(
            GetLanguageService(),
            GetUserConfigService(),
            GetBackupJobRepository(),
            GetBackupService(),
            new BusinessSoftwareMonitor(),
            GetProgressWriter());
    }

    /// <summary>
    /// Creates a new instance of LanguageOrchestrationService.
    /// </summary>
    public static LanguageService CreateLanguageOrchestrationService()
    {
        return new LanguageService(
            GetLanguageService(),
            GetUserConfigService());
    }

    /// <summary>
    /// Creates a new instance of ConfigOrchestrationService.
    /// </summary>
    public static ConfigService CreateConfigOrchestrationService()
    {
        return new ConfigService(GetUserConfigService());
    }

    /// <summary>
    /// Resets all cached instances (useful for testing).
    /// </summary>
    public static void Reset()
    {
        _languageServiceInstance = null;
        _userConfigServiceInstance = null;
        _backupJobRepositoryInstance = null;
        _backupServiceInstance = null;
        _progressWriterInstance = null;
    }
}
