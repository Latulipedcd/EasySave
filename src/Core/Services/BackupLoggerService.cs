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

    public BackupLoggerService(ILog logService)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
    }

    public void Configure(LogFormat format)
        => _logService.Configure(format);

    public void LogSourceNotFound(string backupName, string sourceDirectory)
    {
        _logService.LogBackup(new LogEntry
        {
            BackupName = backupName,
            Source = "Path not found",
            Target = "Path not found or cannot be created",
            Duration = TimeSpan.Zero,
            Timestamp = DateTime.Now,
            FileSize = 0,
            WorkType = WorkType.file_transfer,
            ErrorMessage = $"Source directory does not exist: {sourceDirectory}"
        });
    }

    public void LogBusinessSoftwareBlock(string backupName, string sourceDirectory, string targetDirectory)
    {
        _logService.LogBackup(new LogEntry
        {
            BackupName = backupName,
            Source = PathHelper.ToUncPath(sourceDirectory),
            Target = PathHelper.ToUncPath(targetDirectory),
            Duration = TimeSpan.Zero,
            Timestamp = DateTime.Now,
            FileSize = 0,
            WorkType = WorkType.file_transfer,
            ErrorMessage = "Backup stopped due to running business software."
        });
    }

    public void LogDirectoryCreation(string backupName, string sourceFile, string folderPath, TimeSpan duration)
    {
        _logService.LogBackup(new LogEntry
        {
            BackupName = backupName,
            Source = PathHelper.ToUncPath(sourceFile),
            Target = PathHelper.ToUncPath(folderPath),
            Duration = duration,
            Timestamp = DateTime.Now,
            FileSize = 0,
            WorkType = WorkType.folder_creation
        });
    }

    public void LogFileOperation(string backupName, string sourceFile, string targetPath,
                                 TimeSpan duration, long fileSize, bool wasEncrypted, long encryptionTimeMs)
    {
        _logService.LogBackup(new LogEntry
        {
            BackupName = backupName,
            Source = PathHelper.ToUncPath(sourceFile),
            Target = PathHelper.ToUncPath(targetPath),
            Duration = duration,
            Timestamp = DateTime.Now,
            FileSize = fileSize,
            WorkType = wasEncrypted ? WorkType.encryption : WorkType.file_transfer,
            EncryptionTimeMs = encryptionTimeMs
        });
    }
}
