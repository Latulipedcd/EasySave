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
    /// Thread synchronization event used to pause and resume the job. 
    /// Unblocks execution when set (true); blocks execution when reset (false).
    /// </summary>
    public ManualResetEventSlim PauseEvent { get; } = new(true);

    /// <summary>
    /// The Task running this job's ExecuteBackupAsync call.
    /// Stored here so the orchestrator can await it in Task.WhenAll and so
    /// callers can inspect its status (Running, Completed, Faulted, Cancelled).
    /// </summary>
    public Task<BackupState>? ExecutionTask { get; set; }

    public bool ManuallyPaused { get; set; }

    public bool BusinessPaused { get; set; }

    /// <summary>
    /// Initializes a new instance of the execution handle.
    /// </summary>
    /// <param name="jobName">The name of the job to track.</param>
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

    /// <summary>
    /// Releases the internal wait handles used by the cancellation token and pause event.
    /// </summary>
    public void Dispose()
    {
        Cts.Dispose();
        PauseEvent.Dispose();
    }
}
