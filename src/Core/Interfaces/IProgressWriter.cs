using Core.Models;
using Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    public interface IProgressWriter
    {
        void Write(BackupState backupState);
    }
}
