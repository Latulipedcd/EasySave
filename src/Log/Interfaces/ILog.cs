using Log.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Log.Interfaces
{
    public interface ILog
    {
        void Configure(LogFormat format, string? folder = null);
        void LogBackup(Object entry);
    }
}
