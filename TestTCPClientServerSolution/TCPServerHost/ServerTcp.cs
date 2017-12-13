using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public IDictionary<int, HandlerSocket> HandlerSockets = new Dictionary<int, HandlerSocket>();
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
                Debug.WriteLine(socketException.Message);
            }

        }

        public void Stop()
        {
            lock (HandlerSockets)
            {
                foreach (var item in HandlerSockets.Values)
                {
                    if(item.Socket.Connected)
                        item.Socket.Close();
                }
                HandlerSockets.Clear();
            }
        }

        private void _acceptSocketOnCompleted(object sender, SocketAsyncEventArgs socketArgs)
        {
            if(socketArgs.SocketError != SocketError.Success) return;

            lock (HandlerSockets)
            {
                Interlocked.Increment(ref _currentClientNum);
                var hSocket = new HandlerSocket(socketArgs.AcceptSocket, _currentClientNum);
                HandlerSockets.Add(_currentClientNum, hSocket);
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
                Debug.WriteLine(socketException.Message);
            }

        }

        private void _awaitRecieveData(int clientNum)
        {
            HandlerSocket rcvSocket;
            lock (HandlerSockets)
            {
                if(!HandlerSockets.ContainsKey(clientNum)) return;
                rcvSocket = HandlerSockets[clientNum];
            }

            try
            {  
                var p = new Packet(rcvSocket, clientNum);
                rcvSocket.Socket.BeginReceive(p.Buffer, 0, p.Buffer.Length, SocketFlags.None, _receiveData, p);
            }
            catch (SocketException socketException)
            {
                Debug.WriteLine(socketException.Message);
            }
            catch (Exception e)
            {
                var messageError = e.InnerException?.Message ?? e.Message;
                Debug.WriteLine(messageError);
            }
        }

        private void _receiveData(IAsyncResult result)
        {
            var data = (Packet) result.AsyncState;
            int size = data.Socket.Socket.EndReceive(result);
            int clientNum = data.Socket.ClientId;

            if (data.Buffer[0] == 0x1)
            {
                data.Socket.IsBusy = true;
                Task.Run(() => _sendData(data));
            }
            else if (data.Buffer[0] == 0xA)
            {
                data.Socket.IsBusy = false;
            }
            _awaitRecieveData(clientNum);
        }

        private void _sendData(Packet pack)
        {
            var sock = pack.Socket.Socket;
            while (sock.Connected && pack.Socket.IsBusy)
            {
                var rnd = new Random();
                var args = new SocketAsyncEventArgs();
                byte[] buffer = new byte[3];
                rnd.NextBytes(buffer);
                args.SetBuffer(buffer, 0, buffer.Length);
                sock.Send(buffer);
                Thread.Sleep(50);
            }
        }

        #region Properties

        public Action<int> OnClientConnect { get; set; }
        public Action<int> OnClientDisconnect { get; set; }

        #endregion

        public class HandlerSocket
        {
            public HandlerSocket(Socket socket, int clientId)
            {
                Socket = socket;
                ClientId = clientId;
            }
            public Socket Socket { get; }
            public int ClientId { get; }
            public bool IsBusy { get; set; }
        }

        private class Packet
        {
            public Packet(HandlerSocket socket, int clientId)
            {
                Socket = socket;
                ClientId = clientId;
            }

            public HandlerSocket Socket { get; }
            public int ClientId { get; }

            public readonly byte[] Buffer = new byte[1];
        }
    }
}
