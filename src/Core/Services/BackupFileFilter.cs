using Core.Enums;
using Core.Interfaces;
using Core.Models;
using System.Collections.Generic;
using System.IO;

namespace Core.Services;

/// <summary>
/// Pure stateless filter: decides whether a file belongs in the current backup pass.
/// </summary>
public sealed class BackupFileFilter : IBackupFileFilter
{
    public bool ShouldProcess(BackupJob job, string sourceFile, string targetPath)
    {
        if (job.Type != BackupType.Differencial)
            return true;

        if (!File.Exists(targetPath))
            return true;

        return new FileInfo(sourceFile).LastWriteTime > new FileInfo(targetPath).LastWriteTime;
    }

    public bool IsPriorityFile(string filePath, IReadOnlyList<string> priorityExtensions)
    {
        if (priorityExtensions == null || priorityExtensions.Count == 0)
            return false;

        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        return priorityExtensions.Contains(ext);
    }
}
