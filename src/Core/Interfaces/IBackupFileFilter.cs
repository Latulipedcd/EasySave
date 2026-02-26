using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// Decides whether a given file should be included in the current backup pass.
/// Centralises differential-backup logic and priority-extension matching so that
/// BackupService does not need to know the rules.
/// </summary>
public interface IBackupFileFilter
{
    /// <summary>
    /// Returns true when the file must be copied.
    /// Always true for Full backups.
    /// For Differential backups, true only when the target is absent or older than the source.
    /// </summary>
    /// <param name="job">The current backup job context, which dictates if the backup is Full or Differential.</param>
    /// <param name="sourceFile">The full path of the source file being evaluated.</param>
    /// <param name="targetPath">The full path of the intended destination file to check against.</param>
    /// <returns><c>true</c> if the file meets the criteria to be backed up; otherwise, <c>false</c>.</returns>
    bool ShouldProcess(BackupJob job, string sourceFile, string targetPath);

    /// <summary>
    /// Returns true when the file's extension is in <paramref name="priorityExtensions"/>.
    /// Extensions in the list must already be normalised (lowercase, leading dot, e.g. ".txt").
    /// </summary>
    /// /// <param name="filePath">The path or name of the file to check for a priority extension.</param>
    /// <param name="priorityExtensions">A read-only list of configured priority extensions.</param>
    /// <returns><c>true</c> if the file matches one of the priority extensions; otherwise, <c>false</c>.</returns>
    bool IsPriorityFile(string filePath, IReadOnlyList<string> priorityExtensions);
}
