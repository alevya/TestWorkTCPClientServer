using System;

namespace TCPServerHost
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int portNum = 11111;
            var srv = new ServerTcp();
            srv.Start(portNum);
            //var srv = new Server();
            //srv.Start(portNum);
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
            Console.WriteLine($"Press any key for exit");
            Console.ReadLine();
            srv.Stop();
        }
    }
}
