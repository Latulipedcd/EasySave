using Core.Enums;
using Core.Interfaces;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Log.Interfaces;
using Log.Services;
using Log.Enums;

namespace Core.Services
{
    public class BackupService : IBackupService
    {
        private readonly ILog _logService;
        private readonly IFileService _fileService;
        private readonly ICopyService _copyService;
        private readonly IProgressWriter _progressWriter;
        private readonly IBusinessSoftwareMonitor _businessSoftwareMonitor;

        /// <summary>
        /// Async-compatible mutex that lets exactly one job process a file at a time.
        /// SemaphoreSlim(1, 1) means: capacity of 1, initially 1 slot available.
        /// A regular lock cannot be used here because:
        ///   - lock blocks the calling thread while waiting.
        ///   - In async code this wastes a thread pool thread instead of releasing it.
        ///   - The C# compiler also forbids await inside a lock block.
        /// SemaphoreSlim.WaitAsync() suspends the task without occupying a thread,
        /// and accepts a CancellationToken so a Stop request can interrupt the wait.
        /// </summary>
        private static readonly SemaphoreSlim _fileProcessingGate = new(1, 1);

        public BackupService(
            ILog logService,
            IFileService fileService,
            ICopyService copyService,
            IProgressWriter progressWriter,
            IBusinessSoftwareMonitor businessSoftwareMonitor
            )
        {
            _logService = logService;
            _fileService = fileService;
            _copyService = copyService;
            _progressWriter = progressWriter;
            _businessSoftwareMonitor = businessSoftwareMonitor;
        }

        // Semaphore to ensure only one CryptoSoft process runs at a time across all jobs.
        // Implemented as a SemaphoreSlim with capacity 1 (mono-instance).
        private static readonly SemaphoreSlim _cryptoSemaphore = new(1, 1);

        public BackupState ExecuteBackup(BackupJob job, LogFormat format, string? businessSoftware, List<string> CryptoSoftExtensions, string? cryptoSoftPath)
        {
            // Configure log format early
            _logService.Configure(format);

            // Early exit if source directory doesn't exist
            if (!Directory.Exists(job.SourceDirectory))
            {
                return HandleSourceDirectoryNotFound(job);
            }

            var state = InitializeBackupState(job, format, out var files);

            // Early exit if business software is blocking
            if (CheckBusinessSoftwareBlocking(job, businessSoftware, state))
                return state;

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(job.SourceDirectory, file);
                var targetPath = Path.Combine(job.TargetDirectory, relativePath);

                // Create target directory if needed
                CreateTargetDirectory(job, file, targetPath);

                // Check if file should be processed (differential backup logic)
                if (!ShouldProcessFile(job, targetPath, file))
                {
                    state.FilesRemaining--;
                    continue;
                }

                // Process the file (copy or encrypt)
                var stopwatch = Stopwatch.StartNew();
                bool success = ProcessFile(file, targetPath, CryptoSoftExtensions, cryptoSoftPath, out bool wasEncrypted, out long encryptionTimeMs, CancellationToken.None);
                stopwatch.Stop();

                if (!success)
                    state.Status = BackupStatus.Error;

                // Log and update progress
                var fileInfo = new FileInfo(file);
                LogFileOperation(job, file, targetPath, stopwatch.Elapsed, fileInfo.Length, wasEncrypted, encryptionTimeMs);

                state.FilesRemaining--;
                state.BytesRemaining -= fileInfo.Length;
                state.CurrentFileSource = file;
                state.CurrentFileTarget = targetPath;

                UpdateProgress(job, state);
            }

            FinalizeBackup(job, state);
            return state;
        }



        /// <summary>
        /// Executes a backup job asynchronously with support for pause, cancellation,
        /// priority file rules, and large-file bandwidth control.
        /// </summary>
        /// <param name="cancellationToken">
        /// Provided by CancellationTokenSource.Token (one per job in JobExecutionHandle).
        /// Calling Cts.Cancel() signals this token, which causes ThrowIfCancellationRequested()
        /// or WaitAsync(token) to throw OperationCanceledException, stopping the job cleanly.
        /// </param>
        /// <param name="pauseEvent">
        /// A ManualResetEventSlim that starts in Set state (not blocking).
        /// Reset() blocks any thread/task that calls Wait() on it — used to pause the job.
        /// Set() unblocks all waiters — used to resume.
        /// The async version polls it with Task.Delay instead of calling the blocking Wait()
        /// so the thread is not held while the job is paused.
        /// </param>
        /// <param name="executionContext">
        /// Shared state for the current batch: large-file semaphore and priority coordinator.
        /// Same instance is passed to every concurrent job so they can coordinate.
        /// </param>
        public async Task<BackupState> ExecuteBackupAsync(BackupJob job, LogFormat format, List<string> CryptoSoftExtensions, string? cryptoSoftPath, CancellationToken cancellationToken, ManualResetEventSlim pauseEvent, SharedExecutionContext executionContext)
        {
            _logService.Configure(format);

            if (!Directory.Exists(job.SourceDirectory))
            {
                executionContext.UnregisterJob(job.Name);
                return HandleSourceDirectoryNotFound(job);
            }

            var state = InitializeBackupState(job, format, out var files);

            bool hasPriorityRules = executionContext.PriorityExtensions.Count > 0;

            // Build the set of priority files for this job upfront so we only scan once.
            var prioritySet = hasPriorityRules
                ? new HashSet<string>(
                    files.Where(f => IsPriorityFile(f, executionContext.PriorityExtensions)),
                    StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>();

            // Register how many priority files this job owns so other jobs can wait if needed.
            executionContext.RegisterJob(job.Name, prioritySet.Count);

            foreach (var file in files)
            {
                // ── Async pause check: poll until resumed or cancelled ──────────────────
                if (!pauseEvent.IsSet)
                {
                    state.Status = BackupStatus.Paused;
                    UpdateProgress(job, state);
                }

                try
                {
                    while (!pauseEvent.IsSet)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    state.Status = BackupStatus.Cancelled;
                    executionContext.UnregisterJob(job.Name);
                    FinalizeBackup(job, state);
                    return state;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    state.Status = BackupStatus.Cancelled;
                    executionContext.UnregisterJob(job.Name);
                    FinalizeBackup(job, state);
                    return state;
                }

                if (state.Status == BackupStatus.Paused)
                {
                    state.Status = BackupStatus.Active;
                    UpdateProgress(job, state);
                }

                // ── Rule 1: Priority extensions ────────────────────────────────────────
                // Non-priority files must wait while any job still has priority files pending.
                bool isPriority = prioritySet.Contains(file);

                if (!isPriority && hasPriorityRules)
                {
                    try
                    {
                        while (executionContext.HasAnyPriorityFilePending)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await Task.Delay(50, cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        state.Status = BackupStatus.Cancelled;
                        executionContext.UnregisterJob(job.Name);
                        FinalizeBackup(job, state);
                        return state;
                    }
                }

                // ── Rule 2: Large-file bandwidth gate ──────────────────────────────────
                // Only one file larger than MaxParallelFileSizeBytes can transfer at a time.
                // Small files skip this gate and run freely in parallel.
                long fileSize = new FileInfo(file).Length;
                bool isLargeFile = executionContext.MaxParallelFileSizeBytes > 0
                                   && fileSize > executionContext.MaxParallelFileSizeBytes;

                if (isLargeFile)
                {
                    try
                    {
                        await executionContext.LargeFileSemaphore.WaitAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        state.Status = BackupStatus.Cancelled;
                        executionContext.UnregisterJob(job.Name);
                        FinalizeBackup(job, state);
                        return state;
                    }
                }

                // ── File processing ────────────────────────────────────────────────────
                // finally always runs: releases the large-file semaphore (if held) and
                // decrements the priority counter (if this is a priority file).
                try
                {
                    var relativePath = Path.GetRelativePath(job.SourceDirectory, file);
                    var targetPath = Path.Combine(job.TargetDirectory, relativePath);

                    CreateTargetDirectory(job, file, targetPath);

                    if (!ShouldProcessFile(job, targetPath, file))
                    {
                        // File is already up-to-date (differential backup). Count it as done.
                        state.FilesRemaining--;
                        continue; // finally will decrement priority + release semaphore
                    }

                    var stopwatch = Stopwatch.StartNew();
                    bool success = ProcessFile(file, targetPath, CryptoSoftExtensions, cryptoSoftPath, out bool wasEncrypted, out long encryptionTimeMs, cancellationToken);
                    stopwatch.Stop();

                    if (!success)
                        state.Status = BackupStatus.Error;

                    var fileInfo = new FileInfo(file);
                    LogFileOperation(job, file, targetPath, stopwatch.Elapsed, fileInfo.Length, wasEncrypted, encryptionTimeMs);

                    state.FilesRemaining--;
                    state.BytesRemaining -= fileInfo.Length;
                    state.CurrentFileSource = file;
                    state.CurrentFileTarget = targetPath;

                    UpdateProgress(job, state);
                }
                finally
                {
                    // Always release the large-file slot so the next waiter can proceed.
                    if (isLargeFile)
                        executionContext.LargeFileSemaphore.Release();

                    // Always mark this priority file as done, even if it was skipped.
                    if (isPriority)
                        executionContext.DecrementPriority(job.Name);
                }

                // Yield to let other async tasks take their turn.
                await Task.Yield();
            }

            executionContext.UnregisterJob(job.Name);
            FinalizeBackup(job, state);
            return state;
        }

        /// <summary>
        /// Initializes the backup state with file discovery and totals.
        /// </summary>
        private BackupState InitializeBackupState(BackupJob job, LogFormat format, out string[] files)
        {
            files = _fileService.GetFiles(job.SourceDirectory);
            long totalBytes = files.Sum(f => new FileInfo(f).Length);

            _logService.Configure(format);

            return new BackupState(job)
            {
                Status = BackupStatus.Active,
                TotalFiles = files.Length,
                FilesRemaining = files.Length,
                TotalBytes = totalBytes,
                BytesRemaining = totalBytes
            };
        }

        /// <summary>
        /// Handles the case where the source directory doesn't exist.
        /// </summary>
        /// <returns>BackupState with error status.</returns>
        private BackupState HandleSourceDirectoryNotFound(BackupJob job)
        {
            var errorState = new BackupState(job)
            {
                Status = BackupStatus.Error,
                TimeStamp = DateTime.Now,
                ErrorMessage = $"Source directory does not exist: {job.SourceDirectory}"
            };

            var logError = new LogEntry
            {
                BackupName = job.Name,
                Source = "Path not found",
                Target = "Path not found or cannot be created",
                Duration = TimeSpan.Zero,
                Timestamp = DateTime.Now,
                FileSize = 0,
                WorkType = WorkType.file_transfer,
                ErrorMessage = $"Source directory does not exist: {job.SourceDirectory}"
            };
            _logService.LogBackup(logError);
            _progressWriter.Write(errorState);

            return errorState;
        }

        /// <summary>
        /// Checks if business software is blocking the backup.
        /// </summary>
        /// <returns>True if backup should be blocked, false otherwise.</returns>
        private bool CheckBusinessSoftwareBlocking(BackupJob job, string? businessSoftware, BackupState state)
        {
            if (businessSoftware == null)
                return false;

            if (!_businessSoftwareMonitor.IsBusinessSoftwareRunning(businessSoftware))
                return false;

            // Business software is running - block backup
            state.Status = BackupStatus.Error;

            var logError = new LogEntry
            {
                BackupName = job.Name,
                Source = PathHelper.ToUncPath(job.SourceDirectory),
                Target = PathHelper.ToUncPath(job.TargetDirectory),
                Duration = TimeSpan.Zero,
                Timestamp = DateTime.Now,
                FileSize = 0,
                WorkType = WorkType.file_transfer,
                ErrorMessage = "Backup stopped due to running business software."
            };
            _logService.LogBackup(logError);

            var errorState = new BackupState(job)
            {
                Status = BackupStatus.Error,
                TimeStamp = DateTime.Now,
                ErrorMessage = "Backup stopped due to running business software."
            };
            _progressWriter.Write(errorState);

            return true;
        }

        /// <summary>
        /// Creates the target directory if it doesn't exist and logs the operation.
        /// </summary>
        private void CreateTargetDirectory(BackupJob job, string sourceFile, string targetPath)
        {
            var folderPath = Path.GetDirectoryName(targetPath)!;
            if (Directory.Exists(folderPath))
                return;

            var stopwatch = Stopwatch.StartNew();
            Directory.CreateDirectory(folderPath);
            stopwatch.Stop();

            var logEntryFolder = new LogEntry
            {
                BackupName = job.Name,
                Source = PathHelper.ToUncPath(sourceFile),
                Target = PathHelper.ToUncPath(folderPath),
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.Now,
                FileSize = 0,
                WorkType = WorkType.folder_creation
            };
            _logService.LogBackup(logEntryFolder);
        }

        /// <summary>
        /// Determines if a file should be processed based on backup type (differential logic).
        /// </summary>
        private static bool ShouldProcessFile(BackupJob job, string targetPath, string sourceFile)
        {
            if (job.Type != BackupType.Differencial)
                return true;

            if (!File.Exists(targetPath))
                return true;

            var sourceInfo = new FileInfo(sourceFile);
            var targetInfo = new FileInfo(targetPath);

            return sourceInfo.LastWriteTime > targetInfo.LastWriteTime;
        }

        /// <summary>
        /// Processes a single file - either encrypts or copies it.
        /// </summary>
        /// <param name="wasEncrypted">Output parameter indicating if the file was encrypted.</param>
        /// <param name="encryptionTimeMs">Output parameter with encryption time: 0=no encryption, >0=time in ms, <0=error code.</param>
        /// <returns>True if operation succeeded, false otherwise.</returns>
        private bool ProcessFile(string sourceFile, string targetPath, List<string> cryptoExtensions, string? cryptoSoftPath, out bool wasEncrypted, out long encryptionTimeMs, CancellationToken cancellationToken)
        {
            wasEncrypted = RequiresEncryption(sourceFile, cryptoExtensions);

            if (wasEncrypted)
            {
                // Ensure only one CryptoSoft process runs at a time across all jobs.
                try
                {
                    _cryptoSemaphore.Wait(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    encryptionTimeMs = -99; // cancelled while waiting for crypto slot
                    return false;
                }

                try
                {
                    return EncryptFile(sourceFile, targetPath, cryptoSoftPath, out encryptionTimeMs);
                }
                finally
                {
                    try { _cryptoSemaphore.Release(); } catch { }
                }
            }
            else
            {
                encryptionTimeMs = 0; // No encryption
                return _copyService.CopyFiles(sourceFile, targetPath);
            }
        }

        /// <summary>
        /// Logs a file operation (copy or encryption).
        /// </summary>
        private void LogFileOperation(BackupJob job, string sourceFile, string targetPath, TimeSpan duration, long fileSize, bool wasEncrypted, long encryptionTimeMs)
        {
            var logEntry = new LogEntry
            {
                BackupName = job.Name,
                Source = PathHelper.ToUncPath(sourceFile),
                Target = PathHelper.ToUncPath(targetPath),
                Duration = duration,
                Timestamp = DateTime.Now,
                FileSize = fileSize,
                WorkType = wasEncrypted ? WorkType.encryption : WorkType.file_transfer,
                EncryptionTimeMs = encryptionTimeMs
            };
            _logService.LogBackup(logEntry);
        }

        /// <summary>
        /// Updates progress information after processing a file.
        /// </summary>
        private void UpdateProgress(BackupJob job, BackupState state)
        {
            var progressState = new BackupState(job)
            {
                Status = state.Status,
                TimeStamp = DateTime.Now,
                TotalFiles = state.TotalFiles,
                FilesRemaining = state.FilesRemaining,
                TotalBytes = state.TotalBytes,
                BytesRemaining = state.BytesRemaining,
                CurrentFileSource = state.CurrentFileSource,
                CurrentFileTarget = state.CurrentFileTarget
            };

            _progressWriter.Write(progressState);
        }

        /// <summary>
        /// Finalizes the backup by setting completion status and writing final progress.
        /// </summary>
        private void FinalizeBackup(BackupJob job, BackupState state)
        {
            if (state.Status != BackupStatus.Error && state.Status != BackupStatus.Cancelled)
                state.Status = BackupStatus.Completed;

            var resetInfo = new BackupState(job)
            {
                Status = state.Status,
                TimeStamp = DateTime.Now,
                TotalFiles = 0,
                FilesRemaining = 0,
                TotalBytes = 0,
                BytesRemaining = 0,
                CurrentFileSource = null,
                CurrentFileTarget = null
            };

            _progressWriter.Write(resetInfo);
        }

        /// <summary>
        /// Returns true if the file's extension is in the configured priority list.
        /// Extensions in the context are already normalised (lowercase, leading dot).
        /// </summary>
        private static bool IsPriorityFile(string filePath, IReadOnlyList<string> priorityExtensions)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return priorityExtensions.Contains(ext);
        }

        /// <summary>
        /// Checks if a file requires encryption based on its extension.
        /// </summary>
        private static bool RequiresEncryption(string filePath, List<string> cryptoExtensions)
        {
            if (cryptoExtensions == null || cryptoExtensions.Count == 0)
                return false;

            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            // Normalize extensions in the list to lowercase and ensure they start with a dot
            return cryptoExtensions.Any(ext => 
            {
                string normalizedExt = ext.ToLowerInvariant();
                if (!normalizedExt.StartsWith('.'))
                    normalizedExt = '.' + normalizedExt;

                return normalizedExt == extension;
            });
        }

        /// <summary>
        /// Encrypts a file using CryptoSoft.exe and saves it to the target path.
        /// </summary>
        /// <param name="encryptionTimeMs">Output: 0=no encryption, >0=time in ms, <0=error code (-1=path error, -2=process error, -3=exit code error, -99=exception)</param>
        private bool EncryptFile(string sourceFilePath, string targetFilePath, string? cryptoSoftPath, out long encryptionTimeMs)
        {
            // DEBUG: Log the actual path being used
            Console.WriteLine($"[DEBUG] EncryptFile called with cryptoSoftPath: '{cryptoSoftPath}'");

            // Validate CryptoSoft.exe path
            if (string.IsNullOrEmpty(cryptoSoftPath) || !File.Exists(cryptoSoftPath))
            {
                encryptionTimeMs = -1; // Error code: CryptoSoft.exe not found
                // Fallback to normal copy if CryptoSoft is not available
                return _copyService.CopyFiles(sourceFilePath, targetFilePath);
            }

            var encryptionStopwatch = Stopwatch.StartNew();
            try
            {
                // TODO: Replace "default-key" with actual encryption key from configuration
                var startInfo = new ProcessStartInfo
                {
                    FileName = cryptoSoftPath,
                    Arguments = $"\"{sourceFilePath}\" \"{targetFilePath}\" \"default-key\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    encryptionStopwatch.Stop();
                    encryptionTimeMs = -2; // Error code: Failed to start process
                    return false;
                }

                process.WaitForExit();
                encryptionStopwatch.Stop();

                // Read output for debugging
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    encryptionTimeMs = encryptionStopwatch.ElapsedMilliseconds;
                    return true;
                }
                else
                {
                    // Log the actual exit code as negative value to distinguish from success
                    // This preserves the real error information from CryptoSoft.exe
                    encryptionTimeMs = process.ExitCode < 0 ? process.ExitCode : -process.ExitCode;

                    // Log detailed error information
                    if (!string.IsNullOrEmpty(stderr))
                        Console.WriteLine($"[ENCRYPTION ERROR] CryptoSoft stderr: {stderr}");
                    if (!string.IsNullOrEmpty(stdout))
                        Console.WriteLine($"[ENCRYPTION ERROR] CryptoSoft stdout: {stdout}");

                    return false;
                }
            }
            catch (Exception ex)
            {
                encryptionStopwatch.Stop();
                encryptionTimeMs = -99; // Error code: Exception occurred
                return false;
            }
        }


    }
}
