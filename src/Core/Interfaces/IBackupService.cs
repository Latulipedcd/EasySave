using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.Models;
using Log.Enums;
using Core.Enums;

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
        BackupState ExecuteBackup(BackupJob job, LogFormat format, string? businessSoftware, List<string> CryptoSoftExtensions, string? cryptoSoftPath, LogStorageMode storageMode);

        /// <summary>
        /// Executes a backup job asynchronously with support for pause, cancellation,
        /// priority file rules, and large-file bandwidth control.
        /// </summary>
        Task<BackupState> ExecuteBackupAsync(BackupJob job, LogFormat format, List<string> CryptoSoftExtensions, string? cryptoSoftPath, CancellationToken cancellationToken, ManualResetEventSlim pauseEvent, SharedExecutionContext executionContext, LogStorageMode storageMode);
    }
}

