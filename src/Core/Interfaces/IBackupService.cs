using System;
using System.Collections.Generic;
using System.Text;
using Core.Models;
using Log.Enums;
namespace Core.Interfaces
{
    public interface IBackupService
    {
        BackupState ExecuteBackup(BackupJob job, LogFormat format, string? businessSoftware);
    }
}

