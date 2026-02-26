using Core.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Core.Interfaces;
using Log.Enums;

namespace Core.Services
{
    public class DockerLoggerService : IDockerLoggerService
    {
        private readonly IPAddress _ip;
        private readonly int _port;
        private readonly Socket _socket;
        private bool _isConnected;

        public event Action? ServerUnavailable;

        public DockerLoggerService()
        {
            _ip = IPAddress.Parse("127.0.0.1");
            _port = 11000;
            _isConnected = false;

            _socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
        }

        public void Connect()
        {
            if (_isConnected)
            {
                return;
            }
            try
            {
                _socket.Connect(_ip, _port);
                _isConnected = true;
                Console.WriteLine("Connected to log server");
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Connection failed: " + ex.Message);
                ServerUnavailable?.Invoke();
            }
        }
        

        public void SendLog(LogFormat format, LogEntry entry)
        {
            try
            {
                if (!_isConnected) { Connect(); }

                var logMessage = new Dictionary<string, object>
                {
                    { "Format", format },
                    { "Entry", entry }
                };

                string json = JsonSerializer.Serialize(logMessage);
                string message = json + "<|EOM|>";
                byte[] data = Encoding.UTF8.GetBytes(message);

                _socket.Send(data);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Failed to send log: " + ex.Message);
                _isConnected = false;
                ServerUnavailable?.Invoke();
            }
        }

        //public void Close()
        //{
        //    if (!_isConnected)
        //        return;

        //    _socket.Shutdown(SocketShutdown.Both);
        //    _socket.Close();
        //    _isConnected = false;
        //}
    }
}
