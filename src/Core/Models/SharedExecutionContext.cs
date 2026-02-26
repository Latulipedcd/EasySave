using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Core.Models;

/// <summary>
/// Holds the coordination state shared across all jobs in a single execution batch.
/// </summary>
public sealed class SharedExecutionContext : IDisposable
{
    /// <summary>
    /// Semaphore used to limit concurrent large-file transfers to exactly one at a time.
    /// Small files bypass this gate entirely.
    /// </summary>
    public SemaphoreSlim LargeFileSemaphore { get; } = new(1, 1);

    /// <summary>
    /// Priority file extensions, normalised to lowercase with a leading dot (e.g., ".txt").
    /// An empty list indicates the priority rule is inactive.
    /// </summary>
    public IReadOnlyList<string> PriorityExtensions { get; }

    /// <summary>
    /// The size threshold (in bytes) that triggers the <see cref="LargeFileSemaphore"/>.
    /// A value of 0 indicates the large-file bandwidth rule is disabled.
    /// </summary>
    public long MaxParallelFileSizeBytes { get; }

    // Thread-safe tracker for priority files pending per job.
    private readonly ConcurrentDictionary<string, int> _pendingPriorityPerJob = new();

    public SharedExecutionContext(
        IReadOnlyList<string> priorityExtensions,
        long maxParallelFileSizeKb)
    {
        PriorityExtensions = NormalizeExtensions(priorityExtensions);
        MaxParallelFileSizeBytes = maxParallelFileSizeKb > 0 ? maxParallelFileSizeKb * 1024L : 0;
    }

    /// <summary>
    /// Registers a job before its file loop starts, setting its initial count of priority files.
    /// </summary>
    public void RegisterJob(string jobName, int pendingPriorityFileCount)
        => _pendingPriorityPerJob[jobName] = pendingPriorityFileCount;

    /// <summary>
    /// Decrements the priority file count for a job after a priority file is processed or skipped.
    /// </summary>
    public void DecrementPriority(string jobName)
        => _pendingPriorityPerJob.AddOrUpdate(jobName, 0, (_, current) => Math.Max(0, current - 1));

    /// <summary>
    /// Removes a job from tracking once it finishes, cancels, or faults.
    /// </summary>
    public void UnregisterJob(string jobName)
        => _pendingPriorityPerJob.TryRemove(jobName, out _);

    /// <summary>
    /// Gets a value indicating whether any registered job still has priority files pending.
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
