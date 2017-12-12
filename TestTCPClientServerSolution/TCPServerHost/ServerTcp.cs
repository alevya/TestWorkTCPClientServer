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
                Console.WriteLine(socketException);
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
                Console.WriteLine(socketException);
                
            }

        }

        private void _awaitRecieveData(int clientNum)
        {
            HandlerSocket rcvSocket = null;
            lock (HandlerSockets)
            {
                if(!HandlerSockets.ContainsKey(clientNum)) return;
                rcvSocket = HandlerSockets[clientNum];
            }

            try
            {
                //var argsRecieveSocket = new SocketAsyncEventArgs();
                //argsRecieveSocket.Completed += _recieveSocketOnCompleted;
                //argsRecieveSocket.AcceptSocket = rcvSocket.Socket;
                //var buffer = new byte[1];
                //argsRecieveSocket.SetBuffer(buffer, 0, buffer.Length);
                //rcvSocket.Socket.ReceiveAsync(argsRecieveSocket);
                var p = new Packet(rcvSocket, clientNum);
                rcvSocket.Socket.BeginReceive(p.Buffer, 0, p.Buffer.Length, SocketFlags.None, _receiveData, p);
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

        private void _receiveData(IAsyncResult result)
        {
            var data = (Packet) result.AsyncState;
            int size = data.Socket.Socket.EndReceive(result);
            int clientNum = data.Socket.ClientId;

            if (data.Buffer[0] == 0x1)
            {
                //var rnd = new Random();

                //var args = new SocketAsyncEventArgs();
                //byte[] buffer = new byte[3];
                //rnd.NextBytes(buffer);
                //args.SetBuffer(buffer, 0, buffer.Length);
                //socketArgs.AcceptSocket.SendAsync(args);
                data.Socket.IsLoop = true;
                Task.Run(() => _sendData(data));

            }
            else if (data.Buffer[0] == 0xA)
            {
                data.Socket.IsLoop = false;
            }
            _awaitRecieveData(clientNum);
        }

        private void _recieveSocketOnCompleted(object sender, SocketAsyncEventArgs socketArgs)
        {
            if(socketArgs.SocketError != SocketError.Success) return;

            if (socketArgs.Buffer[0] == 0x1)
            {
                //var rnd = new Random();

                //var args = new SocketAsyncEventArgs();
                //byte[] buffer = new byte[3];
                //rnd.NextBytes(buffer);
                //args.SetBuffer(buffer, 0, buffer.Length);
                //socketArgs.AcceptSocket.SendAsync(args);
                Task.Run(() => _sendData(socketArgs));

            }
            else if (socketArgs.Buffer[0] == 0xA)
            {

            }
            _awaitRecieveData(1);
        }


        private void _sendData(SocketAsyncEventArgs socketArgs)
        {
            while (socketArgs.Buffer[0] != 0xA)
            {
                var rnd = new Random();

                var args = new SocketAsyncEventArgs();
                byte[] buffer = new byte[3];
                rnd.NextBytes(buffer);
                args.SetBuffer(buffer, 0, buffer.Length);
                socketArgs.AcceptSocket.Send(buffer);
                Thread.Sleep(50);
            }
        }

        private void _sendData(Packet pack)
        {
            while (pack.Socket.IsLoop)
            {
                var rnd = new Random();

                var args = new SocketAsyncEventArgs();
                byte[] buffer = new byte[3];
                rnd.NextBytes(buffer);
                args.SetBuffer(buffer, 0, buffer.Length);
                pack.Socket.Socket.Send(buffer);
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
            public bool IsLoop { get; set; }
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

            public byte[] Buffer = new byte[1];
        }
    }
}
