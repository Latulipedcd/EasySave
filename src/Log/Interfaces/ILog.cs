using Log.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Log.Interfaces
{
    public interface ILog
    {
        void Configure(LogFormat format);
        void LogBackup(Object entry);
    }
}
