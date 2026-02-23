using Core.Models;
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
        public void Connect();

        public void SendLog(LogEntry entry);

        //public void Close();
    }
}
