using Core.Models;
using System.Collections.Generic;

namespace EasySave.Application.Interfaces;

/// <summary>
/// Application-layer abstraction for reading the latest in-memory progress snapshots.
/// UI layers can depend on this without referencing Core service interfaces.
/// </summary>
public interface IJobProgressSnapshotSource
{
    IReadOnlyList<BackupState> GetStatesSnapshot();
}
