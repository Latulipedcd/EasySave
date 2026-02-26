using Core.Interfaces;
using Core.Models;
using EasySave.Application.Interfaces;
using System;
using System.Collections.Generic;

namespace EasySave.Application.Services;

/// <summary>
/// Bridges Core's progress snapshot source to the Application abstraction.
/// </summary>
public sealed class JobProgressSnapshotSourceAdapter : IJobProgressSnapshotSource
{
    private readonly IProgressSnapshotSource _inner;

    public JobProgressSnapshotSourceAdapter(IProgressSnapshotSource inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyList<BackupState> GetStatesSnapshot() => _inner.GetStatesSnapshot();
}
