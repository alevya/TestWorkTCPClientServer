using System;
using System.Configuration;

namespace TCPServerHost
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var appSettings = ConfigurationManager.AppSettings;
            var sPortName = appSettings.Get("port") ?? "11111";
            if (!int.TryParse(sPortName, out int portNum))
            {
                Console.WriteLine("Port number error");
                Console.ReadKey();
                return;
            }
            
            var srv = new ServerTcp();
            srv.Start(portNum);
           
            srv.OnClientConnect += delegate(int id) { Console.WriteLine($"Client id = {id} connected"); }; 
            srv.OnClientDisconnect += delegate(int id)
            {
                lock (srv.HandlerSockets)
                {
                    if (srv.HandlerSockets.ContainsKey(id))
                    {
                        srv.HandlerSockets[id].Socket.Close();
                        srv.HandlerSockets.Remove(id);
                    }
                }
                Console.WriteLine($"Client id = {id} disconnected");
            };
            Console.WriteLine($"Server start. Listening on port {portNum}...");
            Console.WriteLine("Press any key for exit");
            Console.ReadKey();
            srv.Stop();
        }
    }
}
