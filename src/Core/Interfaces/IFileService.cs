using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for file system operations.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Retrieves all files from a directory recursively.
        /// </summary>
        /// <param name="path">The directory path to search.</param>
        /// <returns>An array of full file paths.</returns>
        string[] GetFiles(string path);
    }
}
