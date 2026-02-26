using Core.Interfaces;
using Core.Repository;
using Core.Services;
using EasySave.Application.Configuration;
using EasySave.Application.Interfaces;
using EasySave.Application.Services;
using Log.Services;
using System.Net;

namespace EasySave.Application;

/// <summary>
/// Factory for creating application services with all their dependencies wired up.
/// This centralizes the "new" keywords and provides a clean way for UI layers to get services.
/// In a real DI container (like Microsoft.Extensions.DependencyInjection), this would be replaced by container registration.
/// </summary>
public static class ServiceFactory
{
    private static ILanguageService? _languageServiceInstance;
    private static IUserConfigRepository? _userConfigServiceInstance;
    private static IBackupJobRepository? _backupJobRepositoryInstance;
    private static IBackupService? _backupServiceInstance;
    private static IProgressWriter? _progressWriterInstance;
    private static ICopyService? _copyServiceInstance;
    private static IJobStateReader? _jobStateReaderInstance;
    private static IJobProgressSnapshotSource? _jobProgressSnapshotSourceInstance;

    /// <summary>
    /// Gets or creates the singleton IJobStateReader instance.
    /// </summary>
    public static IJobStateReader GetJobStateReader()
    {
        return _jobStateReaderInstance ??= new JobStateFileReader();
    }

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
    public static IUserConfigRepository GetUserConfigService()
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
        // Wrap the JSON writer with an in-memory snapshot for in-process UIs.
        // Core remains responsible only for persistence (state.json).
        return _progressWriterInstance ??= new InMemoryProgressWriter(new ProgressJsonWriter());
    }

    /// <summary>
    /// Gets or creates an application-level in-memory progress snapshot source.
    /// Returns null if the configured progress writer does not support snapshots.
    /// </summary>
    public static IJobProgressSnapshotSource? GetJobProgressSnapshotSource()
    {
        // When GetProgressWriter() returns InMemoryProgressWriter, it also implements IJobProgressSnapshotSource.
        return _jobProgressSnapshotSourceInstance ??= GetProgressWriter() as IJobProgressSnapshotSource;
    }

    /// <summary>
    /// Gets or creates the singleton ICopyService instance.
    /// </summary>
    public static ICopyService GetCopyService()
    {
        return _copyServiceInstance ??= new CopyService();
    }

    /// <summary>
    /// Gets or creates the singleton IBackupService instance.
    /// Wires the shared BackupOperationLogger instance into every sub-service that
    /// needs it (BackupPreflightChecker, BackupDirectoryService, BackupService itself)
    /// so that a single Configure(format) call applies to all three.
    /// </summary>
    public static IBackupService GetBackupService()
    {
        if (_backupServiceInstance != null) return _backupServiceInstance;

        var copyService      = GetCopyService();
        var encryptionService = new EncryptionService(copyService);
        var dockerLogger     = new DockerLoggerService();
        var operationLogger  = new BackupLoggerService(LogService.Instance, dockerLogger);

        _backupServiceInstance = new BackupService(
            new BackupValidationService(new BusinessSoftwareMonitor(), operationLogger),
            new BackupStateService(new FileService(), GetProgressWriter()),
            new BackupDirectoryService(operationLogger),
            new FileTransferService(encryptionService, copyService),
            new BackupFileFilter(),
            operationLogger,
            dockerLogger);

        return _backupServiceInstance;
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
    /// <summary>
    /// Creates a new instance of LanguageService.
    /// </summary>
    public static LanguageService CreateLanguageService()
    {
        return new LanguageService(
            GetLanguageService(),
            GetUserConfigService());
    }

    /// <summary>
    /// <summary>
    /// Creates a new instance of ConfigService.
    /// </summary>
    public static UserConfigService CreateConfigService()
    {
        return new UserConfigService(GetUserConfigService());
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
        _copyServiceInstance = null;
        _jobStateReaderInstance = null;
    }
}
