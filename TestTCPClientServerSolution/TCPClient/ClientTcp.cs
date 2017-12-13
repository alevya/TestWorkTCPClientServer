using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

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
                    AwaitRecieveData();
                }
            }
            catch (SocketException socketException)
            {
                Debug.WriteLine(socketException.Message);
                throw;
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
            argsSend.SetBuffer(data, 0, data.Length);
            _socket.SendAsync(argsSend);
        }

        private void AwaitRecieveData()
        {
            try
            {
                var args = new SocketAsyncEventArgs();
                args.Completed += RecieveOnCompleted;
                var buffer = new byte[4];
                args.SetBuffer(buffer, 0, buffer.Length);
                _socket.ReceiveAsync(args);

            }
            catch (ObjectDisposedException objectDisposedException)
            {
                Debug.WriteLine(objectDisposedException.Message);
            }
            catch (SocketException socketException)
            {
                Debug.WriteLine(socketException.Message);
            }
        }

        private void RecieveOnCompleted(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            OnReceiveData?.Invoke(socketAsyncEventArgs.Buffer, socketAsyncEventArgs.Count);
            AwaitRecieveData();
        }

        #endregion

        #region Properties

        public bool IsConnected => _socket != null && _socket.Connected;

        public Action<byte[], int> OnReceiveData { get; set; }

        #endregion


    }
}
