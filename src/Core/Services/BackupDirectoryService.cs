using Core.Interfaces;
using Core.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace Core.Services;

/// <summary>
/// Default implementation of <see cref="IBackupDirectoryService"/>.
/// Measures directory-creation time with a <see cref="Stopwatch"/> and delegates
/// the log entry to <see cref="IBackupLoggerService"/> so no I/O timing
/// or log construction appears in the backup orchestrator.
/// </summary>
public class BackupDirectoryService : IBackupDirectoryService
{
    private readonly IBackupLoggerService _logger;

    public BackupDirectoryService(IBackupLoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Derives the parent directory of <paramref name="targetPath"/> and returns
    /// immediately if it already exists.
    /// Otherwise creates all missing path segments, measures the elapsed time,
    /// and writes a directory-creation log entry.
    /// </summary>
    public void EnsureTargetDirectory(BackupJob job, string sourceFile, string targetPath)
    {
        var folderPath = Path.GetDirectoryName(targetPath)!;
        if (Directory.Exists(folderPath))
            return;

        var sw = Stopwatch.StartNew();
        Directory.CreateDirectory(folderPath);
        sw.Stop();

        _logger.LogDirectoryCreation(job.Name, sourceFile, folderPath, sw.Elapsed);
    }
}
