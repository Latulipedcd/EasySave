using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Log.Enums;
using Log.Interfaces;
using System;

namespace Core.Services;

/// <summary>
/// Builds structured <see cref="LogEntry"/> objects and forwards them to the underlying
/// <see cref="ILog"/> writer.  Centralises UNC path conversion and WorkType assignment
/// so that BackupService never constructs log entries directly.
/// </summary>
public sealed class BackupLoggerService : IBackupLoggerService
{
    private readonly ILog _logService;
    private readonly IDockerLoggerService _dockerLoggerService;
    private bool _dockerFailed = false;

    public BackupLoggerService(ILog logService, IDockerLoggerService dockerLoggerService)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _dockerLoggerService = dockerLoggerService ?? throw new ArgumentNullException(nameof(dockerLoggerService));

        _dockerLoggerService.ServerUnavailable += OnDockerServerUnavailable;
    }

    private void OnDockerServerUnavailable()
    {
        _dockerFailed = true;
    }

    public void Configure(LogFormat format)
        => _logService.Configure(format);

    private void WriteLog(LogFormat format, LogStorageMode storageMode, LogEntry entry)
    {
        bool shouldWriteLocal = storageMode != LogStorageMode.Docker || _dockerFailed;
        bool shouldWriteDocker = storageMode != LogStorageMode.Local && !_dockerFailed;

        if (shouldWriteLocal)
            _logService.LogBackup(entry);

        if (shouldWriteDocker)
            _dockerLoggerService.SendLog(format, entry);
    }

    public void LogSourceNotFound(LogFormat format, LogStorageMode storageMode, string backupName, string sourceDirectory)
    {

        var entry = new LogEntry
        {
            BackupName = backupName,
            Source = "Path not found",
            Target = "Path not found or cannot be created",
            Duration = TimeSpan.Zero,
            Timestamp = DateTime.Now,
            FileSize = 0,
            WorkType = WorkType.file_transfer,
            ErrorMessage = $"Source directory does not exist: {sourceDirectory}",
            UserName = Environment.UserName
        };

        WriteLog(format, storageMode, entry);
    }

    public void LogBusinessSoftwareBlock(LogFormat format, LogStorageMode storageMode, string backupName, string sourceDirectory, string targetDirectory)
    {
        var entry = new LogEntry
        {
            BackupName = backupName,
            Source = PathHelper.ToUncPath(sourceDirectory),
            Target = PathHelper.ToUncPath(targetDirectory),
            Duration = TimeSpan.Zero,
            Timestamp = DateTime.Now,
            FileSize = 0,
            WorkType = WorkType.file_transfer,
            ErrorMessage = "Backup stopped due to running business software.",
            UserName = Environment.UserName
        };

        WriteLog(format, storageMode, entry);
    }

    public void LogDirectoryCreation(LogFormat format, LogStorageMode storageMode, string backupName, string sourceFile, string folderPath, TimeSpan duration)
    {
        var entry = new LogEntry
        {
            BackupName = backupName,
            Source = PathHelper.ToUncPath(sourceFile),
            Target = PathHelper.ToUncPath(folderPath),
            Duration = duration,
            Timestamp = DateTime.Now,
            FileSize = 0,
            WorkType = WorkType.folder_creation,
            UserName = Environment.UserName
        };

        WriteLog(format, storageMode, entry);
    }

    public void LogFileOperation(LogFormat format, LogStorageMode storageMode, string backupName, string sourceFile, string targetPath,
                                 TimeSpan duration, long fileSize, bool wasEncrypted, long encryptionTimeMs)
    {
        var entry = new LogEntry
        {
            BackupName = backupName,
            Source = PathHelper.ToUncPath(sourceFile),
            Target = PathHelper.ToUncPath(targetPath),
            Duration = duration,
            Timestamp = DateTime.Now,
            FileSize = fileSize,
            WorkType = wasEncrypted ? WorkType.encryption : WorkType.file_transfer,
            EncryptionTimeMs = encryptionTimeMs,
            UserName = Environment.UserName
        };

        WriteLog(format, storageMode, entry);
    }
}
