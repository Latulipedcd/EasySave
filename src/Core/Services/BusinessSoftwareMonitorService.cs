using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Core.Services
{
    /// <summary>
    /// Service that monitors running processes to detect business software.
    /// </summary>
    public class BusinessSoftwareMonitor : IBusinessSoftwareMonitor
    {
        /// <summary>
        /// Checks if a process with the specified name is currently running.
        /// </summary>
        /// <param name="processName">The name of the process to check (without .exe extension).</param>
        /// <returns>True if at least one instance of the process is running, false otherwise.</returns>
        public bool IsBusinessSoftwareRunning(string processName)
        {
            return Process.GetProcessesByName(processName).Any(); // Check if any process with the given name is running
        }
    }
}
