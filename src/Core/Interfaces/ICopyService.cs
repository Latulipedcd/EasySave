using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for file copy operations.
    /// </summary>
    public interface ICopyService
    {
        /// <summary>
        /// Copies a file from source to destination.
        /// </summary>
        /// <param name="file">The source file path.</param>
        /// <param name="destination">The destination file path.</param>
        /// <returns>True if the copy succeeded, false otherwise.</returns>
        bool CopyFiles(string file, string destination);
    }
}
