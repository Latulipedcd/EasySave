using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Enums;

namespace Core.Models;

/// <summary>
/// Tracks per-job execution state including cancellation, pause, and business software pause.
/// Used to control running backup jobs.
/// </summary>
public class JobExecutionHandle : IDisposable
{
    public string JobName { get; }

    /// <summary>
    /// CancellationTokenSource owns a token that can be passed to async operations.
    /// Calling Cts.Cancel() signals that token: any WaitAsync(token) or
    /// ThrowIfCancellationRequested() inside the job will immediately throw
    /// OperationCanceledException, stopping the job at its next checkpoint.
    /// Must be Disposed after use to release the internal WaitHandle.
    /// </summary>
    public CancellationTokenSource Cts { get; } = new();

    /// <summary>
    /// ManualResetEventSlim is a lightweight signal with two states:
    ///   Set   (IsSet = true)  → any task polling it proceeds normally.
    ///   Reset (IsSet = false) → any task polling it waits until Set() is called.
    /// Initialized to true (not blocking). Reset() is called to pause the job,
    /// Set() to resume it. Two independent flags (ManuallyPaused / BusinessPaused)
    /// control it so both causes of pause must be cleared before the job resumes.
    /// "Slim" means it avoids allocating a kernel WaitHandle until actually needed.
    /// Must be Disposed after use.
    /// </summary>
    public ManualResetEventSlim PauseEvent { get; } = new(true);

    /// <summary>
    /// The Task running this job's ExecuteBackupAsync call.
    /// Stored here so the orchestrator can await it in Task.WhenAll and so
    /// callers can inspect its status (Running, Completed, Faulted, Cancelled).
    /// </summary>
    public Task<BackupState>? ExecutionTask { get; set; }

    /// <summary>Set by the user via PauseJob / PauseAllJobs.</summary>
    public bool ManuallyPaused { get; set; }

    /// <summary>Set by the business software monitor thread when the configured process is running.</summary>
    public bool BusinessPaused { get; set; }

    public JobExecutionHandle(string jobName)
    {
        JobName = jobName;
    }

    /// <summary>
    /// Recalculates the pause event state based on manual and business software pause flags.
    /// Resets (blocks) if either flag is true, sets (unblocks) if both are false.
    /// </summary>
    public void UpdatePauseState()
    {
        if (ManuallyPaused || BusinessPaused)
            PauseEvent.Reset();
        else
            PauseEvent.Set();
    }

    public void Dispose()
    {
        Cts.Dispose();
        PauseEvent.Dispose();
    }
}
