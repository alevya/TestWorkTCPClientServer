using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
                argsAcceptSocket.Completed += AcceptSocketOnCompleted;
                _listenerSocket.AcceptAsync(argsAcceptSocket);
            }
            catch (SocketException socketException)
            {
                Console.WriteLine(socketException.Message);
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

        private void AcceptSocketOnCompleted(object sender, SocketAsyncEventArgs socketArgs)
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
                AwaitRecieveData(_currentClientNum);
                var argsAcceptSocket = new SocketAsyncEventArgs();
                argsAcceptSocket.Completed += AcceptSocketOnCompleted;
                _listenerSocket.AcceptAsync(argsAcceptSocket);
            }
            catch (SocketException socketException)
            {
                OnClientDisconnect?.Invoke(_currentClientNum);
                Debug.WriteLine(socketException.Message);
            }

        }

        private void AwaitRecieveData(int clientNum)
        {
            HandlerSocket rcvSocket;
            lock (HandlerSockets)
            {
                if(!HandlerSockets.ContainsKey(clientNum)) return;
                rcvSocket = HandlerSockets[clientNum];
            }

            try
            {  
                var p = new Packet(rcvSocket);
                rcvSocket.Socket.BeginReceive(p.Buffer, 0, p.Buffer.Length, SocketFlags.None, ReceiveData, p);
            }
            catch (SocketException socketException)
            {
                OnClientDisconnect?.Invoke(clientNum);
                Debug.WriteLine(socketException.Message);
            }
            catch (Exception e)
            {
                var messageError = e.InnerException?.Message ?? e.Message;
                Debug.WriteLine(messageError);
            }
        }

        private void ReceiveData(IAsyncResult result)
        {
            var data = (Packet) result.AsyncState;
            int clientNum = data.Socket.ClientId;
            try
            {
                int size = data.Socket.Socket.EndReceive(result);
                switch (data.Buffer[0])
                {
                    case 0x1:
                        data.Socket.IsBusy = true;
                        Task.Run(() => SendData(data));
                        break;
                    case 0xA:
                        data.Socket.IsBusy = false;
                        break;
                    default:
                        break;
                }
                AwaitRecieveData(clientNum);

            }
            catch (SocketException socketException)
            {
                OnClientDisconnect(clientNum);
                Debug.WriteLine(socketException.Message);
            }

        }

        private static void SendData(Packet pack)
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
            public Packet(HandlerSocket socket)
            {
                Socket = socket;
            }
            public HandlerSocket Socket { get; }
            public readonly byte[] Buffer = new byte[1];
        }
    }
}
