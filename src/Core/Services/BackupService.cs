using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Log.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Services;

/// <summary>
/// Orchestrates a single backup job end-to-end.
/// All cross-cutting concerns are delegated to focused single-responsibility services:
/// pre-flight validation to <see cref="IBackupValidationService"/>,
/// state lifecycle to <see cref="IBackupStateService"/>,
/// directory management to <see cref="IBackupDirectoryService"/>,
/// file transfer (encrypt-or-copy) to <see cref="IFileTransferService"/>,
/// differential and priority filtering to <see cref="IBackupFileFilter"/>,
/// and file-level log entries to <see cref="IBackupLoggerService"/>.
/// </summary>
public class BackupService : IBackupService
{
    private readonly IBackupValidationService _preflightChecker;
    private readonly IBackupStateService _stateService;
    private readonly IBackupDirectoryService _directoryService;
    private readonly IFileTransferService _transferService;
    private readonly IBackupFileFilter _fileFilter;
    private readonly IBackupLoggerService _logger;
    private readonly IDockerLoggerService _dockerLogger;

    public BackupService(
        IBackupValidationService preflightChecker,
        IBackupStateService stateService,
        IBackupDirectoryService directoryService,
        IFileTransferService transferService,
        IBackupFileFilter fileFilter,
        IBackupLoggerService logger,
        IDockerLoggerService dockerLogger)
    {
        _preflightChecker = preflightChecker ?? throw new ArgumentNullException(nameof(preflightChecker));
        _stateService     = stateService     ?? throw new ArgumentNullException(nameof(stateService));
        _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
        _transferService  = transferService  ?? throw new ArgumentNullException(nameof(transferService));
        _fileFilter       = fileFilter       ?? throw new ArgumentNullException(nameof(fileFilter));
        _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));
        _dockerLogger     = dockerLogger     ?? throw new ArgumentNullException(nameof(dockerLogger));
    }

    // ── Public entry points ──────────────────────────────────────────────────

    /// <summary>
    /// Synchronous backup entry point intended for console/non-async callers.
    /// Runs pre-flight checks (source directory, business software), then iterates
    /// all source files sequentially — transferring each, updating counters, and
    /// writing a progress snapshot after every file.
    /// Does not support pause or cancellation; use <see cref="ExecuteBackupAsync"/> for that.
    /// </summary>
    public BackupState ExecuteBackup(BackupJob job, LogFormat format, string? businessSoftware,
                                     List<string> cryptoSoftExtensions, string? cryptoSoftPath, LogStorageMode storageMode)
    {
        _logger.Configure(format);

        if (_preflightChecker.IsSourceDirectoryMissing(job, storageMode, format))
            return _stateService.CreateError(job, $"Source directory does not exist: {job.SourceDirectory}");

        if (_preflightChecker.IsBlockedByBusinessSoftware(job, businessSoftware, storageMode, format))
            return _stateService.CreateError(job, "Backup stopped due to running business software.");

        var (state, files) = _stateService.Initialize(job);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(job.SourceDirectory, file);
            var targetPath   = Path.Combine(job.TargetDirectory, relativePath);

            _directoryService.EnsureTargetDirectory(job, file, targetPath, storageMode, format);

            if (!_fileFilter.ShouldProcess(job, file, targetPath))
            {
                state.FilesRemaining--;
                continue;
            }

            long fileSize = new FileInfo(file).Length;
            bool success  = _transferService.Transfer(
                                file, targetPath, cryptoSoftExtensions, cryptoSoftPath,
                                CancellationToken.None,
                                out bool wasEncrypted, out long encryptionTimeMs, out TimeSpan duration);

            if (!success)
                state.Status = BackupStatus.Error;

            _logger.LogFileOperation(format, storageMode, job.Name, file, targetPath, duration, fileSize, wasEncrypted, encryptionTimeMs);

            state.FilesRemaining--;
            state.BytesRemaining   -= fileSize;
            state.CurrentFileSource = file;
            state.CurrentFileTarget = targetPath;

            _stateService.WriteProgress(job, state);
        }

        _stateService.Finalize(job, state);
        return state;
    }

    /// <summary>
    /// Asynchronous backup entry point with full cooperative pause/cancel support.
    /// Checks <paramref name="pauseEvent"/> (a <see cref="ManualResetEventSlim"/>) before
    /// every file and suspends without blocking a thread-pool thread until the event is set.
    /// Monitors <paramref name="cancellationToken"/> at every suspension point so a stop
    /// request is honoured promptly.
    /// Priority files (matched by extension from <paramref name="executionContext"/>) are
    /// processed first; non-priority files spin-wait until no job has pending priority work.
    /// Files exceeding <see cref="SharedExecutionContext.MaxParallelFileSizeBytes"/> are
    /// serialised through <see cref="SharedExecutionContext.LargeFileSemaphore"/> to cap
    /// peak memory when multiple jobs run concurrently.
    /// </summary>
    public async Task<BackupState> ExecuteBackupAsync(
        BackupJob job, LogFormat format,
        List<string> cryptoSoftExtensions, string? cryptoSoftPath,
        CancellationToken cancellationToken,
        ManualResetEventSlim pauseEvent,
        SharedExecutionContext executionContext,
       LogStorageMode storageMode)
    {
        _logger.Configure(format);

        if (_preflightChecker.IsSourceDirectoryMissing(job, storageMode, format))
        {
            executionContext.UnregisterJob(job.Name);
            return _stateService.CreateError(job, $"Source directory does not exist: {job.SourceDirectory}");
        }

        var (state, files) = _stateService.Initialize(job);

        // Build the priority set once before the loop to avoid repeated LINQ per file.
        bool hasPriorityRules = executionContext.PriorityExtensions.Count > 0;
        var prioritySet = hasPriorityRules
            ? new HashSet<string>(
                files.Where(f => _fileFilter.IsPriorityFile(f, executionContext.PriorityExtensions)),
                StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>();

        executionContext.RegisterJob(job.Name, prioritySet.Count);

        foreach (var file in files)
        {
            // ── Cooperative pause ──────────────────────────────────────────────
            // Write a Paused snapshot before entering the wait loop so that UI
            // reflects the real state immediately rather than on the next file.
            if (!pauseEvent.IsSet)
            {
                state.Status = BackupStatus.Paused;
                _stateService.WriteProgress(job, state);
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
                return CancelJob(job, state, executionContext);
            }

            if (cancellationToken.IsCancellationRequested)
                return CancelJob(job, state, executionContext);

            // Restore Active status after a resume so the UI stops showing Paused.
            if (state.Status == BackupStatus.Paused)
            {
                state.Status = BackupStatus.Active;
                _stateService.WriteProgress(job, state);
            }

            // ── Priority gate ──────────────────────────────────────────────────
            // Non-priority files wait here until every job has exhausted its priority
            // file queue, ensuring priority work is done across all parallel jobs first.
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
                    return CancelJob(job, state, executionContext);
                }
            }

            // ── Large-file gate ────────────────────────────────────────────────
            // Serialise transfers above the size threshold so peak memory usage is
            // bounded when many jobs run concurrently on large datasets.
            long fileSize    = new FileInfo(file).Length;
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
                    return CancelJob(job, state, executionContext);
                }
            }

            // ── File processing ────────────────────────────────────────────────
            try
            {
                var relativePath = Path.GetRelativePath(job.SourceDirectory, file);
                var targetPath   = Path.Combine(job.TargetDirectory, relativePath);

                _directoryService.EnsureTargetDirectory(job, file, targetPath, storageMode, format);

                if (!_fileFilter.ShouldProcess(job, file, targetPath))
                {
                    state.FilesRemaining--;
                    continue;
                }

                bool success = _transferService.Transfer(
                                   file, targetPath, cryptoSoftExtensions, cryptoSoftPath,
                                   cancellationToken,
                                   out bool wasEncrypted, out long encryptionTimeMs, out TimeSpan duration);

                if (!success)
                    state.Status = BackupStatus.Error;

                _logger.LogFileOperation(format, storageMode, job.Name, file, targetPath, duration, fileSize, wasEncrypted, encryptionTimeMs);

                state.FilesRemaining--;
                state.BytesRemaining   -= fileSize;
                state.CurrentFileSource = file;
                state.CurrentFileTarget = targetPath;

                _stateService.WriteProgress(job, state);
            }
            finally
            {
                // Release gates in finally so they are freed even if an exception occurs.
                if (isLargeFile)
                    executionContext.LargeFileSemaphore.Release();

                if (isPriority)
                    executionContext.DecrementPriority(job.Name);
            }

            // Yield after each file so other concurrent jobs get CPU time and
            // the thread pool is not monopolised by a single long-running backup.
            await Task.Yield();
        }

        executionContext.UnregisterJob(job.Name);
        _stateService.Finalize(job, state);
        return state;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Transitions the job to <see cref="BackupStatus.Cancelled"/>, unregisters it from
    /// the shared execution context so priority and large-file counters are released,
    /// and writes a final zeroed progress snapshot via <see cref="IBackupStateService.Finalize"/>.
    /// Called from every <see cref="OperationCanceledException"/> catch and from every
    /// explicit cancellation-token check inside the file loop.
    /// </summary>
    private BackupState CancelJob(BackupJob job, BackupState state, SharedExecutionContext executionContext)
    {
        state.Status = BackupStatus.Cancelled;
        executionContext.UnregisterJob(job.Name);
        _stateService.Finalize(job, state);
        return state;
    }
}
