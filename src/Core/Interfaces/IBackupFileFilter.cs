using System.Collections.Generic;
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
    bool ShouldProcess(BackupJob job, string sourceFile, string targetPath);

    /// <summary>
    /// Returns true when the file's extension is in <paramref name="priorityExtensions"/>.
    /// Extensions in the list must already be normalised (lowercase, leading dot, e.g. ".txt").
    /// </summary>
    bool IsPriorityFile(string filePath, IReadOnlyList<string> priorityExtensions);
}
