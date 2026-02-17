using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    /// <summary>
    /// Service for file system operations and file discovery.
    /// </summary>
    public class FileService : IFileService
    {
        public FileService() { }

        /// <summary>
        /// Recursively retrieves all files from a directory and its subdirectories.
        /// </summary>
        /// <param name="path">The root directory path to search for files.</param>
        /// <returns>An array of full file paths found in the directory and all subdirectories.</returns>
        public string[] GetFiles(string path)
        {
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        }
    }

   
}
