using Core.Interfaces;
using Core.Models;
using EasySave.Application.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Application.Services;

/// <summary>
/// Decorates a progress writer with an in-memory snapshot for in-process UIs.
/// This keeps snapshot concerns out of Core while still writing state.json via the inner writer.
/// </summary>
public sealed class InMemoryProgressWriter : IProgressWriter, IJobProgressSnapshotSource
{
    /// <summary>
    /// The underlying writer responsible for persistence (e.g. writing state.json).
    /// </summary>
    private readonly IProgressWriter _inner;

    /// <summary>
    /// Latest known job states keyed by job name.
    /// </summary>
    private readonly ConcurrentDictionary<string, BackupState> _states = new(StringComparer.Ordinal);

    public InMemoryProgressWriter(IProgressWriter inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>
    /// Updates the in-memory snapshot for the job and forwards the state to the inner writer
    /// (typically persisting it to state.json).
    /// </summary>
    public void Write(BackupState backupState)
    {
        if (backupState == null)
            throw new ArgumentNullException(nameof(backupState));

        if (backupState.Job?.Name != null)
            _states[backupState.Job.Name] = backupState;

        _inner.Write(backupState);
    }

    /// <summary>
    /// Clears the in-memory snapshot and forwards the clear to the inner writer.
    /// </summary>
    public void Clear()
    {
        _states.Clear();
        _inner.Clear();
    }

    /// <summary>
    /// Returns a point-in-time snapshot of all known job states.
    /// </summary>
    public IReadOnlyList<BackupState> GetStatesSnapshot()
        => _states.Values.ToList();
}
