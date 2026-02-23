using Log.Interfaces;
using Log.Services;
using Log.Enums;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace LogServer
{
    internal class LogServer
    {
        private ILog _logService;

        public Socket StartServer()
        {
            Console.WriteLine("Test start");
            _logService = LogService.Instance;
            _logService.Configure(LogFormat.Json, "./logs/EasyLog");

            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 11_000);
            Socket serverSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(ipEndPoint);

            serverSocket.Listen();

            return serverSocket;
        }

        public Socket AcceptConnection(Socket socket)
        {
                Console.WriteLine("Test avant accept");
                return socket.Accept();
                Console.WriteLine("Test après accept");
        }

        public void ListenToClient(Socket client)
        {
            try
            {

                while (true) {
                    Console.WriteLine("Test listen");
                    byte[] buffer = new byte[4096];
                    int received = client.Receive(buffer, SocketFlags.None);

                    string response = Encoding.UTF8.GetString(buffer, 0, received);

                    var eom = "<|EOM|>";
                    if (response.Contains(eom))
                    {
                        string cleanJson = response.Replace(eom, "");

                        LogEntry entry = JsonSerializer.Deserialize<LogEntry>(cleanJson);

                        client.Send(Encoding.UTF8.GetBytes(response.ToUpper()));

                        if (entry != null)
                        {
                            _logService.LogBackup(entry);
                            Console.WriteLine("Log écrit.");
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Disconnect(Socket socket)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}
