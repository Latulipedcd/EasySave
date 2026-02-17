using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for monitoring business software processes.
    /// </summary>
    public interface IBusinessSoftwareMonitor
    {
        /// <summary>
        /// Checks if a specific business software process is currently running.
        /// </summary>
        /// <param name="process">The name of the process to check.</param>
        /// <returns>True if the process is running, false otherwise.</returns>
        bool IsBusinessSoftwareRunning(string process);
    }

}
