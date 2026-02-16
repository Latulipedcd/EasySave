using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Core.Services
{
    public class BusinessSoftwareMonitor : IBusinessSoftwareMonitor
    {

        public bool IsBusinessSoftwareRunning(string processName)
        {
            return Process.GetProcessesByName(processName).Any(); // Check if any process with the given name is running
        }
    }
}
