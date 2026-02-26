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
    private readonly IProgressWriter _inner;
    private readonly ConcurrentDictionary<string, BackupState> _states = new(StringComparer.Ordinal);

    public InMemoryProgressWriter(IProgressWriter inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public void Write(BackupState backupState)
    {
        if (backupState?.Job?.Name != null)
            _states[backupState.Job.Name] = backupState;

        _inner.Write(backupState);
    }

    public void Clear()
    {
        _states.Clear();
        _inner.Clear();
    }

    public IReadOnlyList<BackupState> GetStatesSnapshot()
        => _states.Values.ToList();
}
