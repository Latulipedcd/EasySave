using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    public interface IBackupJobRepository
    {
        IReadOnlyList<BackupJob> GetAll();
        void Add(BackupJob job);
        void Update(BackupJob job);
        void Delete(string jobName);
    }
}
