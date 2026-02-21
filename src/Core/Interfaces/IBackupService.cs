using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.Models;
using Log.Enums;
namespace Core.Interfaces
{
    /// <summary>
    /// Interface for backup execution service.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Executes a backup job with the specified configuration.
        /// </summary>
        /// <param name="job">The backup job to execute.</param>
        /// <param name="format">The log format to use.</param>
        /// <param name="businessSoftware">Optional business software process name to monitor.</param>
        /// <param name="CryptoSoftExtensions">List of file extensions that require encryption.</param>
        /// <param name="cryptoSoftPath">Path to the encryption software executable.</param>
        /// <returns>The final backup state after execution.</returns>
        BackupState ExecuteBackup(BackupJob job, LogFormat format, string? businessSoftware, List<string> CryptoSoftExtensions, string? cryptoSoftPath);

        /// <summary>
        /// Executes a backup job asynchronously with support for pause and cancellation.
        /// Yields between files to allow cooperative interleaving with other jobs.
        /// Business software monitoring is handled externally via the pauseEvent.
        /// </summary>
        Task<BackupState> ExecuteBackupAsync(BackupJob job, LogFormat format, List<string> CryptoSoftExtensions, string? cryptoSoftPath, CancellationToken cancellationToken, ManualResetEventSlim pauseEvent);
    }
}

