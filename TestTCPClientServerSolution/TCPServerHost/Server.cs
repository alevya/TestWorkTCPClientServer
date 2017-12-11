using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPServerHost
{
    internal class Server
    {
        private Socket _listenerSocket;

        public void Start(int listenPort)
        {
            try
            {
                //var host = Dns.GetHostEntry("127.0.0.1");
                //var ipAddrList = host.AddressList;
                //if (!ipAddrList.Any()) return;

                //var ipAddr = ipAddrList[0];
                var ePoint = new IPEndPoint(IPAddress.Any, listenPort);
                _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listenerSocket.Bind(ePoint);
                _listenerSocket.Listen(10);

                var args = new SocketAsyncEventArgs();
                args.Completed += ArgsOnCompleted;
                _listenerSocket.AcceptAsync(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        private void ArgsOnCompleted(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            
        }
    }
}
