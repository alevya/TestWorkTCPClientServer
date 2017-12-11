using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPClient
{
    internal class ClientTcp
    {
        private Socket _socket;


        #region Events

        #endregion

        #region Methods

        public void Connect(IPAddress ipAddress, int portNum)
        {
            try
            {
                Disconnect();

                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var ePoint = new IPEndPoint(ipAddress, portNum);
                _socket.Connect(ePoint);
                if (_socket.Connected)
                {
                    _awaitRecieveData();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e); 
            }
        }

        public void Disconnect()
        {
            _socket?.Close();
        }

        private void _awaitRecieveData()
        {
            try
            {
                var args = new SocketAsyncEventArgs();
                args.Completed += _argsOnCompleted;
                _socket.ReceiveAsync(args);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void _argsOnCompleted(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            
        }

        #endregion

        #region Properties

        public bool IsConnected => _socket != null && _socket.Connected;

        #endregion


    }
}
