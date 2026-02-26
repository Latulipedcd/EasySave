using Core.Models;
using Log.Enums;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Core.Interfaces
{
    public interface IDockerLoggerService
    {
        event Action? ServerUnavailable;

        public void Connect();

        public void SendLog(LogFormat format, LogEntry entry);

        //public void Close();
    }
}
