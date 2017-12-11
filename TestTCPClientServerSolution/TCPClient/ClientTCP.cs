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

        public void SendData(byte[] data)
        {
            if (_socket == null) return;
            if (!_socket.Connected) return;

            var argsSend = new SocketAsyncEventArgs();
            argsSend.Completed += _sendOnCompleted;
            argsSend.SetBuffer(data, 0, data.Length);
            _socket.SendAsync(argsSend);
        }

        private void _awaitRecieveData()
        {
            try
            {
                var args = new SocketAsyncEventArgs();
                args.Completed += _recieveOnCompleted;
                _socket.ReceiveAsync(args);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void _sendOnCompleted(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {

        }

        private void _recieveOnCompleted(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            
        }

        #endregion

        #region Properties

        public bool IsConnected => _socket != null && _socket.Connected;

        #endregion


    }
}
