using System.Collections.Generic;
using Core.Models;
using Log.Enums;
using Core.Enums;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for the primary backup execution service.
    /// Responsible for orchestrating the file transfer, filtering, encryption, and state tracking of a backup job.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Executes a backup job synchronously with the specified configuration.
        /// </summary>
        /// <param name="job">The backup job configuration to execute.</param>
        /// <param name="format">The configured log format (e.g., JSON or XML) to use during execution.</param>
        /// <param name="businessSoftware">The optional business software process name to monitor for pausing the backup.</param>
        /// <param name="CryptoSoftExtensions">The list of file extensions that require encryption.</param>
        /// <param name="cryptoSoftPath">The file path to the encryption software executable.</param>
        /// <param name="storageMode">The storage mode preference for logs (e.g., local file, database).</param>
        /// <returns>The final <see cref="BackupState"/> detailing the results after the execution finishes.</returns>
        BackupState ExecuteBackup(BackupJob job, LogFormat format, string? businessSoftware, List<string> CryptoSoftExtensions, string? cryptoSoftPath, LogStorageMode storageMode);

        /// <summary>
        /// Executes a backup job asynchronously with support for pause, cancellation,
        /// priority file rules, and large-file bandwidth control.
        /// </summary>
        /// <param name="job">The backup job configuration to execute.</param>
        /// <param name="format">The configured log format to use during execution.</param>
        /// <param name="CryptoSoftExtensions">The list of file extensions that require encryption.</param>
        /// <param name="cryptoSoftPath">The file path to the encryption software executable.</param>
        /// <param name="cancellationToken">The token used to signal that the backup operation should be stopped/cancelled.</param>
        /// <param name="pauseEvent">A thread-safe event used to pause and resume the backup operation dynamically.</param>
        /// <param name="executionContext">Shared context used to manage global state, priority extensions, and parallel execution limits.</param>
        /// <param name="storageMode">The storage mode preference for logs.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the final <see cref="BackupState"/>.</returns>
        Task<BackupState> ExecuteBackupAsync(BackupJob job, LogFormat format, List<string> CryptoSoftExtensions, string? cryptoSoftPath, CancellationToken cancellationToken, ManualResetEventSlim pauseEvent, SharedExecutionContext executionContext, LogStorageMode storageMode);
    }
}

