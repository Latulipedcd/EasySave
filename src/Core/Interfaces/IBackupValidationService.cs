using Core.Models;
using Core.Enums;
using Log.Enums;

namespace Core.Interfaces;

/// <summary>
/// Validates pre-conditions that must hold before a backup job's file loop begins.
/// Each check logs a structured entry through <see cref="IBackupLoggerService"/>
/// when the condition is violated, so callers do not handle raw log writes.
/// </summary>
public interface IBackupValidationService
{
    /// <summary>
    /// Returns <c>true</c> if <see cref="BackupJob.SourceDirectory"/> does not exist on disk.
    /// Logs the missing-source event when returning <c>true</c>.
    /// </summary>
    /// <param name="job">The backup job containing the source directory to validate.</param>
    /// <param name="storageMode">The configured storage mode used for logging the error event if the directory is missing.</param>
    /// <param name="format">The configured log format to use when logging.</param>
    /// <returns><c>true</c> if the source directory is missing; otherwise, <c>false</c>.</returns>
    bool IsSourceDirectoryMissing(BackupJob job, LogStorageMode storageMode, LogFormat format);

    /// <summary>
    /// Returns <c>true</c> if the named business software is currently running,
    /// which must block the backup from starting.
    /// Always returns <c>false</c> when <paramref name="businessSoftware"/> is <c>null</c>.
    /// Logs the blocking event when returning <c>true</c>.
    /// </summary>
    /// <param name="job">The backup job attempting to execute.</param>
    /// <param name="businessSoftware">The process name of the business software to check against running processes.</param>
    /// <param name="storageMode">The configured storage mode used for logging the block event if the software is running.</param>
    /// <param name="format">The configured log format to use when logging.</param>
    /// <returns><c>true</c> if the specified business software is currently running; otherwise, <c>false</c>.</returns>
    bool IsBlockedByBusinessSoftware(BackupJob job, string? businessSoftware, LogStorageMode storageMode, LogFormat format);
}
