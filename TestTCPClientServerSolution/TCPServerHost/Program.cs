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
            srv.OnClientConnect += delegate(int i) { Console.WriteLine($"Client id = {i} connected"); }; 
            srv.OnClientDisconnect += delegate(int i) { Console.WriteLine($"Client id = {i} disconnected"); };
            Console.WriteLine($"Server start. Listening on port {portNum}...");
            Console.ReadLine();
            srv.Stop();
        }
    }
}
