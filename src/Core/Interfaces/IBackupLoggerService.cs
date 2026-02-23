using Log.Enums;
using System;

namespace Core.Interfaces;

/// <summary>
/// Creates and persists structured log entries for backup operations.
/// Wraps the low-level ILog, handling LogEntry construction, UNC path conversion,
/// and WorkType assignment so that BackupService never builds log entries directly.
/// </summary>
public interface IBackupLoggerService
{
    /// <summary>Sets the output format (JSON / XML) for the underlying log writer.</summary>
    void Configure(LogFormat format);

    /// <summary>Logs a failed start because the source directory does not exist.</summary>
    void LogSourceNotFound(string backupName, string sourceDirectory);

    /// <summary>Logs a backup blocked because the configured business software is running.</summary>
    void LogBusinessSoftwareBlock(string backupName, string sourceDirectory, string targetDirectory);

    /// <summary>Logs the creation of a target subdirectory.</summary>
    void LogDirectoryCreation(string backupName, string sourceFile, string folderPath, TimeSpan duration);

    /// <summary>Logs a completed file transfer (copy or encryption).</summary>
    void LogFileOperation(string backupName, string sourceFile, string targetPath,
                          TimeSpan duration, long fileSize, bool wasEncrypted, long encryptionTimeMs);
}
