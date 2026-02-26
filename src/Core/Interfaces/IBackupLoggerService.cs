using Core.Enums;
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
    /// <summary>
    /// Sets the output format (JSON / XML) for the underlying log writer.
    /// </summary>
    /// <param name="format">The target log format to apply.</param>
    void Configure(LogFormat format);

    /// <summary>
    /// Logs a failed start because the source directory does not exist.
    /// </summary>
    /// <param name="format">The configured log format (e.g., JSON or XML).</param>
    /// <param name="storageMode">The configured storage mode (e.g., local file, database).</param>
    /// <param name="backupName">The name of the backup job that failed to start.</param>
    /// <param name="sourceDirectory">The path to the source directory that could not be found.</param>
    void LogSourceNotFound(LogFormat format, LogStorageMode storageMode, string backupName, string sourceDirectory);

    /// <summary>
    /// Logs a backup blocked because the configured business software is running.
    /// </summary>
    /// <param name="format">The configured log format.</param>
    /// <param name="storageMode">The configured storage mode.</param>
    /// <param name="backupName">The name of the backup job that was blocked.</param>
    /// <param name="sourceDirectory">The source directory of the blocked backup job.</param>
    /// <param name="targetDirectory">The intended target directory of the blocked backup job.</param>
    void LogBusinessSoftwareBlock(LogFormat format, LogStorageMode storageMode, string backupName, string sourceDirectory, string targetDirectory);

    /// <summary>
    /// Logs the creation of a target subdirectory.
    /// </summary>
    /// <param name="format">The configured log format.</param>
    /// <param name="storageMode">The configured storage mode.</param>
    /// <param name="backupName">The name of the backup job.</param>
    /// <param name="sourceFile">The path of the source file that triggered the directory creation.</param>
    /// <param name="folderPath">The full path of the newly created directory.</param>
    /// <param name="duration">The time taken to create the directory.</param>
    void LogDirectoryCreation(LogFormat format, LogStorageMode storageMode, string backupName, string sourceFile, string folderPath, TimeSpan duration);

    /// <summary>
    /// Logs a completed file transfer (copy or encryption).
    /// </summary>
    /// <param name="format">The configured log format.</param>
    /// <param name="storageMode">The configured storage mode.</param>
    /// <param name="backupName">The name of the backup job.</param>
    /// <param name="sourceFile">The full path of the original source file.</param>
    /// <param name="targetPath">The full path of the destination file.</param>
    /// <param name="duration">The total time taken for the file operation.</param>
    /// <param name="fileSize">The size of the transferred file in bytes.</param>
    /// <param name="wasEncrypted">Indicates whether the file underwent encryption via CryptoSoft.</param>
    /// <param name="encryptionTimeMs">The time taken specifically for the encryption process in milliseconds (0 if not encrypted).</param>
    void LogFileOperation(LogFormat format, LogStorageMode storageMode, string backupName, string sourceFile, string targetPath,
                          TimeSpan duration, long fileSize, bool wasEncrypted, long encryptionTimeMs);
}
