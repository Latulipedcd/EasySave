using System;
using System.Collections.Generic;
using System.Text;
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
    }
}

