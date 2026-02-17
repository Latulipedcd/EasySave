using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Core.Interfaces
{
    public interface IBusinessSoftwareMonitor
    {
        bool IsBusinessSoftwareRunning(string process);
    }

}
