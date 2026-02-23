using Core.Enums;
using Core.Interfaces;
using Core.Models;
using System;
using System.IO;

namespace Core.Services;

/// <summary>
/// Default implementation of <see cref="IBackupValidationService"/>.
/// Delegates source-not-found and business-software-block log entries to
/// <see cref="IBackupLoggerService"/> so no log construction happens here.
/// </summary>
public class BackupValidationService : IBackupValidationService
{
    private readonly IBusinessSoftwareMonitor _businessSoftwareMonitor;
    private readonly IBackupLoggerService _logger;

    public BackupValidationService(
        IBusinessSoftwareMonitor businessSoftwareMonitor,
        IBackupLoggerService logger)
    {
        _businessSoftwareMonitor = businessSoftwareMonitor ?? throw new ArgumentNullException(nameof(businessSoftwareMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks whether the job's source directory is present on disk.
    /// Logs the missing-source event before returning <c>true</c>.
    /// </summary>
    public bool IsSourceDirectoryMissing(BackupJob job, LogStorageMode storageMode)
    {
        if (Directory.Exists(job.SourceDirectory))
            return false;

        _logger.LogSourceNotFound(storageMode, job.Name, job.SourceDirectory);
        return true;
    }

    /// <summary>
    /// Checks whether the configured business application is currently running.
    /// Short-circuits to <c>false</c> when <paramref name="businessSoftware"/> is <c>null</c>.
    /// Logs the blocking event before returning <c>true</c>.
    /// </summary>
    public bool IsBlockedByBusinessSoftware(BackupJob job, string? businessSoftware, LogStorageMode storageMode)
    {
        if (businessSoftware == null || !_businessSoftwareMonitor.IsBusinessSoftwareRunning(businessSoftware))
            return false;

        _logger.LogBusinessSoftwareBlock(storageMode, job.Name, job.SourceDirectory, job.TargetDirectory);
        return true;
    }
}
