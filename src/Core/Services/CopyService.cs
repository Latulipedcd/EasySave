using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    /// <summary>
    /// Service responsible for copying files from source to destination.
    /// </summary>
    public class CopyService : ICopyService
    {
        public CopyService() { }

        /// <summary>
        /// Copies a file from source to target location, overwriting if it already exists.
        /// </summary>
        /// <param name="file">The source file path to copy from.</param>
        /// <param name="target">The target file path to copy to.</param>
        /// <returns>True if the copy operation succeeded, false if an error occurred.</returns>
        public bool CopyFiles(string file, string target)
        {
            try
            {
                File.Copy(file, target, true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
