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
    bool RequiresEncryption(string filePath, IReadOnlyList<string> cryptoExtensions);

    /// <summary>
    /// Encrypts <paramref name="sourceFile"/> to <paramref name="targetFile"/> via CryptoSoft.
    /// Blocks until the global CryptoSoft slot is available, then launches the process.
    /// Falls back to a plain copy when the CryptoSoft executable is missing.
    /// </summary>
    /// <param name="encryptionTimeMs">
    /// &gt; 0 = elapsed ms on success;
    /// -1 = CryptoSoft path invalid (fell back to copy);
    /// -2 = process could not start;
    /// -99 = exception or cancellation while waiting for slot.
    /// </param>
    bool Encrypt(string sourceFile, string targetFile, string? cryptoSoftPath,
                 CancellationToken ct, out long encryptionTimeMs);
}
