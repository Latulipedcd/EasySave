using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Core.Models;

/// <summary>
/// Holds the coordination state shared across all jobs in a single execution batch.
/// Created once per batch in JobManagementService and passed to every ExecuteBackupAsync call.
/// Disposed after all jobs in the batch have completed.
/// </summary>
public sealed class SharedExecutionContext : IDisposable
{
    /// <summary>
    /// Limits concurrent large-file transfers to exactly one at a time.
    /// Small files (below MaxParallelFileSizeBytes) bypass this gate entirely
    /// and may run in parallel with each other and with a large-file transfer.
    /// SemaphoreSlim is used instead of lock because the wait must be awaitable
    /// (the C# compiler forbids await inside a lock, and blocking a thread pool
    /// thread with a synchronous wait would defeat the async cooperative design).
    /// </summary>
    public SemaphoreSlim LargeFileSemaphore { get; } = new(1, 1);

    /// <summary>
    /// Priority file extensions, normalised to lowercase with a leading dot (e.g. ".txt").
    /// Empty means the priority rule is not active.
    /// </summary>
    public IReadOnlyList<string> PriorityExtensions { get; }

    /// <summary>
    /// Files strictly larger than this value (in bytes) are subject to LargeFileSemaphore.
    /// 0 means the large-file bandwidth rule is disabled.
    /// </summary>
    public long MaxParallelFileSizeBytes { get; }

    // Per-job count of priority files that have not yet been processed or skipped.
    // Stored in a ConcurrentDictionary because the BS monitor thread and multiple
    // job tasks may read/write it concurrently.
    private readonly ConcurrentDictionary<string, int> _pendingPriorityPerJob = new();

    public SharedExecutionContext(
        IReadOnlyList<string> priorityExtensions,
        long maxParallelFileSizeKb)
    {
        PriorityExtensions = NormalizeExtensions(priorityExtensions);
        MaxParallelFileSizeBytes = maxParallelFileSizeKb > 0 ? maxParallelFileSizeKb * 1024L : 0;
    }

    /// <summary>
    /// Call once per job before its file loop starts, with the number of priority files it owns.
    /// </summary>
    public void RegisterJob(string jobName, int pendingPriorityFileCount)
        => _pendingPriorityPerJob[jobName] = pendingPriorityFileCount;

    /// <summary>
    /// Call each time a priority file has been processed OR skipped by a job.
    /// </summary>
    public void DecrementPriority(string jobName)
        => _pendingPriorityPerJob.AddOrUpdate(jobName, 0, (_, current) => Math.Max(0, current - 1));

    /// <summary>
    /// Call when a job finishes all its files (or is cancelled/errored).
    /// </summary>
    public void UnregisterJob(string jobName)
        => _pendingPriorityPerJob.TryRemove(jobName, out _);

    /// <summary>
    /// True when at least one registered job still has priority files not yet processed or skipped.
    /// Non-priority files must wait while this returns true.
    /// </summary>
    public bool HasAnyPriorityFilePending
        => _pendingPriorityPerJob.Values.Any(c => c > 0);

    public void Dispose() => LargeFileSemaphore.Dispose();

    private static IReadOnlyList<string> NormalizeExtensions(IReadOnlyList<string> raw)
    {
        if (raw == null || raw.Count == 0)
            return Array.Empty<string>();

        return raw
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e =>
            {
                var s = e.Trim().ToLowerInvariant();
                return s.StartsWith('.') ? s : '.' + s;
            })
            .Distinct()
            .ToArray();
    }
}
