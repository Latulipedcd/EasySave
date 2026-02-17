using Core.Models;
using Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for writing backup progress state to persistent storage.
    /// </summary>
    public interface IProgressWriter
    {
        /// <summary>
        /// Writes the backup state to storage.
        /// </summary>
        /// <param name="backupState">The backup state to write.</param>
        void Write(BackupState backupState);
    }
}
