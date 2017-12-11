using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPServerHost
{
    internal class ServerTcp
    {
        private Socket _listenerSocket;
        public IDictionary<int, Socket> HandlerSockets = new Dictionary<int, Socket>();
        private int _currentClientNum;

        public void Start(int listenPort)
        {
            try
            {
                var ePoint = new IPEndPoint(IPAddress.Any, listenPort);
                _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listenerSocket.Bind(ePoint);
                _listenerSocket.Listen(10);

                var argsAcceptSocket = new SocketAsyncEventArgs();
                argsAcceptSocket.Completed += _acceptSocketOnCompleted;
                _listenerSocket.AcceptAsync(argsAcceptSocket);
            }
            catch (SocketException socketException)
            {
                Console.WriteLine(socketException);
            }

        }

        private void _acceptSocketOnCompleted(object sender, SocketAsyncEventArgs socketArgs)
        {
            if(socketArgs.SocketError != SocketError.Success) return;

            lock (HandlerSockets)
            {
                Interlocked.Increment(ref _currentClientNum);
                HandlerSockets.Add(_currentClientNum, socketArgs.AcceptSocket);
                OnClientConnect?.Invoke(_currentClientNum);
            }

            try
            {
                _awaitRecieveData(_currentClientNum);
                var argsAcceptSocket = new SocketAsyncEventArgs();
                argsAcceptSocket.Completed += _acceptSocketOnCompleted;
                _listenerSocket.AcceptAsync(argsAcceptSocket);
            }
            catch (SocketException socketException)
            {
                OnClientDisconnect?.Invoke(_currentClientNum);
                Console.WriteLine(socketException);
                
            }

        }

        private void _awaitRecieveData(int clientNum)
        {
            Socket rcvSocket = null;
            lock (HandlerSockets)
            {
                if(!HandlerSockets.ContainsKey(clientNum)) return;
                rcvSocket = HandlerSockets[clientNum];
            }

            try
            {
                var argsRecieveSocket = new SocketAsyncEventArgs();
                argsRecieveSocket.Completed += _recieveSocketOnCompleted;
                var buffer = new byte[1];
                argsRecieveSocket.SetBuffer(buffer, 0, buffer.Length);
                rcvSocket.ReceiveAsync(argsRecieveSocket);
            }
            catch (SocketException socketException)
            {
                Console.WriteLine(socketException);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void _recieveSocketOnCompleted(object sender, SocketAsyncEventArgs socketArgs)
        {
            
        }

        #region Properties

        public Action<int> OnClientConnect { get; set; }
        public Action<int> OnClientDisconnect { get; set; }

        #endregion
    }
}
