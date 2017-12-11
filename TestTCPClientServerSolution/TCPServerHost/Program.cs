using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPServerHost
{
    class Program
    {
        static void Main(string[] args)
        {
            int portNum = 11111;
            var srv = new Server();
            srv.Start(portNum);

            Console.ReadLine();
        }
    }
}
