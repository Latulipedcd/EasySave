using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Core.Services;

/// <summary>
/// Executes CryptoSoft-based file encryption.
/// The static semaphore ensures that at most one CryptoSoft process runs at a time
/// across ALL concurrent jobs (mono-instance constraint).
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private readonly ICopyService _copyService;

    /// <summary>
    /// Capacity 1: only one caller may hold the slot at a time.
    /// Static so all EncryptionService instances share the same gate.
    /// SemaphoreSlim is used instead of lock because:
    ///   - SemaphoreSlim.Wait(CancellationToken) can be interrupted by Stop requests.
    ///   - The async overload (WaitAsync) allows future callers to not block a thread.
    ///   - The C# compiler forbids await inside a lock block.
    /// </summary>
    private static readonly SemaphoreSlim _cryptoSemaphore = new(1, 1);

    public EncryptionService(ICopyService copyService)
    {
        _copyService = copyService ?? throw new ArgumentNullException(nameof(copyService));
    }

    public bool RequiresEncryption(string filePath, IReadOnlyList<string> cryptoExtensions)
    {
        if (cryptoExtensions == null || cryptoExtensions.Count == 0)
            return false;

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        return cryptoExtensions.Any(ext =>
        {
            string normalized = ext.ToLowerInvariant();
            if (!normalized.StartsWith('.'))
                normalized = '.' + normalized;
            return normalized == extension;
        });
    }

    public bool Encrypt(string sourceFile, string targetFile, string? cryptoSoftPath,
                        CancellationToken ct, out long encryptionTimeMs)
    {
        try
        {
            _cryptoSemaphore.Wait(ct);
        }
        catch (OperationCanceledException)
        {
            encryptionTimeMs = -99;
            return false;
        }

        try
        {
            return RunCryptoSoft(sourceFile, targetFile, cryptoSoftPath, out encryptionTimeMs);
        }
        finally
        {
            try { _cryptoSemaphore.Release(); } catch { }
        }
    }

    private bool RunCryptoSoft(string sourceFile, string targetFile, string? cryptoSoftPath,
                                out long encryptionTimeMs)
    {
        if (string.IsNullOrEmpty(cryptoSoftPath) || !File.Exists(cryptoSoftPath))
        {
            encryptionTimeMs = -1;
            return _copyService.CopyFiles(sourceFile, targetFile);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = cryptoSoftPath,
                Arguments = $"\"{sourceFile}\" \"{targetFile}\" \"default-key\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                sw.Stop();
                encryptionTimeMs = -2;
                return false;
            }

            process.WaitForExit();
            sw.Stop();

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();

            if (process.ExitCode == 0)
            {
                encryptionTimeMs = sw.ElapsedMilliseconds;
                return true;
            }

            encryptionTimeMs = process.ExitCode < 0 ? process.ExitCode : -process.ExitCode;

            if (!string.IsNullOrEmpty(stderr))
                Console.WriteLine($"[ENCRYPTION ERROR] CryptoSoft stderr: {stderr}");
            if (!string.IsNullOrEmpty(stdout))
                Console.WriteLine($"[ENCRYPTION ERROR] CryptoSoft stdout: {stdout}");

            return false;
        }
        catch
        {
            sw.Stop();
            encryptionTimeMs = -99;
            return false;
        }
    }
}
