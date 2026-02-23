using Core.Models;
using Core.Enums;

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
    bool IsSourceDirectoryMissing(BackupJob job, LogStorageMode storageMode);

    /// <summary>
    /// Returns <c>true</c> if the named business software is currently running,
    /// which must block the backup from starting.
    /// Always returns <c>false</c> when <paramref name="businessSoftware"/> is <c>null</c>.
    /// Logs the blocking event when returning <c>true</c>.
    /// </summary>
    bool IsBlockedByBusinessSoftware(BackupJob job, string? businessSoftware, LogStorageMode storageMode);
}
