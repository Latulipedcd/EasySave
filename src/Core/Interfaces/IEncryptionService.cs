using System.Collections.Generic;
using System.Threading;

namespace Core.Interfaces;

/// <summary>
/// Decides whether a file requires encryption and executes the CryptoSoft process.
/// Owns the mono-instance constraint: at most one CryptoSoft process runs at a time
/// across all concurrent jobs, regardless of how many jobs call Encrypt simultaneously.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Returns true if the file's extension matches any entry in <paramref name="cryptoExtensions"/>.
    /// </summary>
    /// <param name="filePath">The full path or file name to evaluate.</param>
    /// <param name="cryptoExtensions">A read-only list of file extensions configured for encryption.</param>
    /// <returns><c>true</c> if the file extension matches one of the specified extensions; otherwise, <c>false</c>.</returns>
    bool RequiresEncryption(string filePath, IReadOnlyList<string> cryptoExtensions);

    /// <summary>
    /// Encrypts <paramref name="sourceFile"/> to <paramref name="targetFile"/> via CryptoSoft.
    /// Blocks until the global CryptoSoft slot is available, then launches the process.
    /// Falls back to a plain copy when the CryptoSoft executable is missing.
    /// </summary>
    /// <param name="sourceFile">The full path to the original unencrypted source file.</param>
    /// <param name="targetFile">The full destination path where the encrypted file should be saved.</param>
    /// <param name="cryptoSoftPath">The full path to the CryptoSoft executable used for encryption.</param>
    /// <param name="ct">The token to monitor for cancellation requests while waiting for the encryption slot or during execution.</param>
    /// <param name="encryptionTimeMs">
    /// &gt; 0 = elapsed ms on success;
    /// -1 = CryptoSoft path invalid (fell back to copy);
    /// -2 = process could not start;
    /// -99 = exception or cancellation while waiting for slot.
    /// -X = exit code of CryptoSoft if problem
    /// </param>
    /// <returns><c>true</c> if the encryption (or fallback copy) completed successfully; otherwise, <c>false</c>.</returns>
    bool Encrypt(string sourceFile, string targetFile, string? cryptoSoftPath,
    CancellationToken ct, out long encryptionTimeMs);
}
