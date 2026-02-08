using System;
using System.Collections.Generic;
using System.Text;
using Core.Models;
namespace Core.Interfaces
{
    public interface IBackupService
    {
        BackupState ExecuteBackup(BackupJob job);
    }
}

