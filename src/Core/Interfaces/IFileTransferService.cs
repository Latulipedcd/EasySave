using System;
using System.Collections.Generic;
using System.Threading;

namespace Core.Interfaces;

/// <summary>
/// Transfers a single file from source to target, choosing between
/// CryptoSoft encryption and plain copy based on the file extension.
/// Measures total elapsed time internally and exposes it to callers
/// so that <see cref="IBackupService"/> implementations need no <c>Stopwatch</c>.
/// </summary>
public interface IFileTransferService
{
    /// <summary>
    /// Transfers <paramref name="sourceFile"/> to <paramref name="targetPath"/>.
    /// When the file extension matches an entry in <paramref name="cryptoExtensions"/>,
    /// CryptoSoft is invoked; otherwise a plain copy is performed.
    /// </summary>
    /// <param name="wasEncrypted"><c>true</c> when CryptoSoft was used for this file.</param>
    /// <param name="encryptionTimeMs">
    /// Wall-clock time reported by CryptoSoft in milliseconds; <c>0</c> for plain copies.
    /// </param>
    /// <param name="duration">Total elapsed time for the entire transfer operation.</param>
    /// <returns><c>true</c> on success, <c>false</c> when the transfer failed.</returns>
    bool Transfer(string sourceFile, string targetPath,
                  IReadOnlyList<string> cryptoExtensions, string? cryptoSoftPath,
                  CancellationToken ct,
                  out bool wasEncrypted, out long encryptionTimeMs, out TimeSpan duration);
}
