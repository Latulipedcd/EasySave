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
/// A named system Mutex ensures that at most one CryptoSoft process runs at a time
/// across ALL processes on the machine (mono-instance constraint).
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private readonly ICopyService _copyService;

    /// <summary>
    /// System-wide named Mutex that serialises CryptoSoft launches across all
    /// EasySave instances (and any other process using the same name).
    /// Named Mutex (prefixed "Global\") is visible across all Windows sessions;
    /// a plain SemaphoreSlim would only guard within a single process instance,
    /// allowing two EasySave instances to each spawn CryptoSoft simultaneously.
    /// The Mutex is never disposed because it must live for the entire process lifetime.
    /// AbandonedMutexException is caught on acquisition: it means a previous holder
    /// crashed without releasing — we recover ownership and continue safely.
    /// </summary>
    private static readonly Mutex _cryptoMutex = new(false, @"Global\EasySave_CryptoSoft_SingleInstance");

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
        // Mutex.WaitOne does not accept a CancellationToken, so we poll with a
        // short timeout interval to remain responsive to Stop requests.
        bool acquired = false;
        try
        {
            while (!acquired)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    acquired = _cryptoMutex.WaitOne(millisecondsTimeout: 500);
                }
                catch (AbandonedMutexException)
                {
                    // Previous holder crashed without releasing; we now own the Mutex.
                    acquired = true;
                }
            }
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
            // ReleaseMutex must be called from the same thread that acquired it.
            // This is guaranteed here because Encrypt is fully synchronous — no
            // await between WaitOne and ReleaseMutex.
            try { _cryptoMutex.ReleaseMutex(); } catch { }
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
