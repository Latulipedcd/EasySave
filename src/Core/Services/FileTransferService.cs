using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Core.Services;

/// <summary>
/// Default implementation of <see cref="IFileTransferService"/>.
/// Consults <see cref="IEncryptionService"/> to decide whether to encrypt or copy,
/// then delegates to the appropriate service.
/// Captures total wall-clock time with a <see cref="Stopwatch"/> and exposes it
/// via <c>out TimeSpan duration</c> so callers have no timing responsibility.
/// </summary>
public class FileTransferService : IFileTransferService
{
    private readonly IEncryptionService _encryptionService;
    private readonly ICopyService _copyService;

    public FileTransferService(IEncryptionService encryptionService, ICopyService copyService)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _copyService = copyService ?? throw new ArgumentNullException(nameof(copyService));
    }

    /// <summary>
    /// Asks <see cref="IEncryptionService.RequiresEncryption"/> whether the source
    /// file's extension is in the crypto list.
    /// If so, invokes <see cref="IEncryptionService.Encrypt"/> (which enforces the
    /// CryptoSoft mono-instance semaphore); otherwise calls <see cref="ICopyService.CopyFiles"/>.
    /// The <see cref="Stopwatch"/> wraps the entire operation so <paramref name="duration"/>
    /// always includes CryptoSoft startup overhead when applicable.
    /// </summary>
    public bool Transfer(string sourceFile, string targetPath,
                         IReadOnlyList<string> cryptoExtensions, string? cryptoSoftPath,
                         CancellationToken ct,
                         out bool wasEncrypted, out long encryptionTimeMs, out TimeSpan duration)
    {
        wasEncrypted = _encryptionService.RequiresEncryption(sourceFile, cryptoExtensions);

        var sw = Stopwatch.StartNew();
        bool success;

        if (wasEncrypted)
            success = _encryptionService.Encrypt(sourceFile, targetPath, cryptoSoftPath, ct, out encryptionTimeMs);
        else
        {
            encryptionTimeMs = 0;
            success = _copyService.CopyFiles(sourceFile, targetPath);
        }

        sw.Stop();
        duration = sw.Elapsed;
        return success;
    }
}
