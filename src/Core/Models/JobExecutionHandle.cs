using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Enums;

namespace Core.Models;

/// <summary>
/// Tracks per-job execution state including cancellation, pause, and business software pause.
/// Used by the orchestrator to control running backup jobs.
/// </summary>
public class JobExecutionHandle : IDisposable
{
    public string JobName { get; }
    public CancellationTokenSource Cts { get; } = new();
    public ManualResetEventSlim PauseEvent { get; } = new(true);
    public Task<BackupState>? ExecutionTask { get; set; }
    public bool ManuallyPaused { get; set; }
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
