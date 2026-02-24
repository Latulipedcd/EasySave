using System;
using System.Net.Sockets;
using System.Text;

namespace LogServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                
                LogServer server = new LogServer();
                Socket serverSocket = server.StartServer();

                while (true)
                {
                    var client = server.AcceptConnection(serverSocket);

                    server.ListenToClient(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}