using Core.Enums;
using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// Ensures that the target directory hierarchy exists before a file is transferred.
/// Measures and logs directory-creation time through <see cref="IBackupLoggerService"/>
/// so that <see cref="IBackupService"/> implementations contain no directory I/O directly.
/// </summary>
public interface IBackupDirectoryService
{
    /// <summary>
    /// Derives the parent directory of <paramref name="targetPath"/> and creates it if absent.
    /// Records elapsed creation time and writes a log entry via <see cref="IBackupLoggerService"/>.
    /// Does nothing when the directory already exists.
    /// </summary>
    void EnsureTargetDirectory(BackupJob job, string sourceFile, string targetPath, LogStorageMode storageMode);
}
