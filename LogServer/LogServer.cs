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
            _logService = LogService.Instance;

            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 11_000);
            Socket serverSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(ipEndPoint);

            serverSocket.Listen();

            return serverSocket;
        }

        public Socket AcceptConnection(Socket socket)
        {
                return socket.Accept();
        }

        public void ListenToClient(Socket client)
        {
            try
            {           
                StringBuilder sb = new StringBuilder();
                byte[] buffer = new byte[4096];
                string eom = "<|EOM|>";

                while (true)
                {
                    int received = client.Receive(buffer);

                    if (received == 0)
                    {
                        break;
                    }

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, received));

                    string content = sb.ToString();

                    int eomIndex;
                    while ((eomIndex = content.IndexOf(eom)) >= 0)
                    {
                        string json = content.Substring(0, eomIndex);
                        content = content.Substring(eomIndex + eom.Length);

                        try
                        {
                            using JsonDocument doc = JsonDocument.Parse(json);

                            JsonElement root = doc.RootElement;

                            LogFormat format = root.GetProperty("Format").Deserialize<LogFormat>();

                            LogEntry entry = root.GetProperty("Entry").Deserialize<LogEntry>();

                            if (entry != null)
                            {
                                _logService.Configure(format);
                                _logService.LogBackup(entry);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("JSON error: " + ex.Message);
                        }
                    }

                    sb.Clear();
                    sb.Append(content);
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
