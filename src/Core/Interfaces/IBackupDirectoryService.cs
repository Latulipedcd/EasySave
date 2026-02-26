using Core.Enums;
using Core.Models;
using Log.Enums;

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
    /// <param name="job">The backup job context associated with the file transfer.</param>
    /// <param name="sourceFile">The full path to the original source file being backed up.</param>
    /// <param name="targetPath">The full path to the intended destination file (used to derive the target directory).</param>
    /// <param name="storageMode">The configured storage mode to use when logging the directory creation event.</param>
    /// <param name="format">The configured log format (e.g., JSON or XML) to use when logging.</param>
    void EnsureTargetDirectory(BackupJob job, string sourceFile, string targetPath, LogStorageMode storageMode, LogFormat format);
}
