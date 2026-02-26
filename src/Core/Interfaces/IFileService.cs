using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    /// <summary>
    /// Abstraction layer for underlying file system operations.
    /// Wraps direct I/O calls to allow for easier unit testing and mocking 
    /// without touching the actual physical disk.
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
