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
        public IDictionary<Guid, HandlerSocket> HandlerSockets = new Dictionary<Guid, HandlerSocket>();
        private Guid _currentClientId;

        /// <summary>
        /// Запуск сервера
        /// </summary>
        /// <param name="listenPort"></param>
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

        /// <summary>
        /// Остановка сервера
        /// </summary>
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

        /// <summary>
        /// Обработчик события при попытке принять входящее соединение
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="socketArgs"></param>
        private void AcceptSocketOnCompleted(object sender, SocketAsyncEventArgs socketArgs)
        {
            if(socketArgs.SocketError != SocketError.Success) return;

            lock (HandlerSockets)
            {
                _currentClientId = Guid.NewGuid();
                var hSocket = new HandlerSocket(socketArgs.AcceptSocket, _currentClientId);
                HandlerSockets.Add(_currentClientId, hSocket);
                OnClientConnect?.Invoke(_currentClientId);
            }

            try
            {
                AwaitRecieveData(_currentClientId);
                var argsAcceptSocket = new SocketAsyncEventArgs();
                argsAcceptSocket.Completed += AcceptSocketOnCompleted;
                _listenerSocket.AcceptAsync(argsAcceptSocket);
            }
            catch (SocketException socketException)
            {
                OnClientDisconnect?.Invoke(_currentClientId);
                Debug.WriteLine(socketException.Message);
            }

        }

        /// <summary>
        /// Ожидание приема команд от клиента
        /// </summary>
        /// <param name="clientId"></param>
        private void AwaitRecieveData(Guid clientId)
        {
            HandlerSocket rcvSocket;
            lock (HandlerSockets)
            {
                if(!HandlerSockets.ContainsKey(clientId)) return;
                rcvSocket = HandlerSockets[clientId];
            }

            try
            {  
                var p = new Packet(rcvSocket);
                rcvSocket.Socket.BeginReceive(p.Buffer, 0, p.Buffer.Length, SocketFlags.None, ReceiveData, p);
            }
            catch (SocketException socketException)
            {
                OnClientDisconnect?.Invoke(clientId);
                Debug.WriteLine(socketException.Message);
            }
            catch (Exception e)
            {
                var messageError = e.InnerException?.Message ?? e.Message;
                Debug.WriteLine(messageError);
            }
        }

        /// <summary>
        /// Обработчик получения команды от клиента
        /// </summary>
        /// <param name="result"></param>
        private void ReceiveData(IAsyncResult result)
        {
            var data = (Packet) result.AsyncState;
            var clientId = data.HSocket.ClientId;
            try
            {
                int size = data.HSocket.Socket.EndReceive(result);
                if(size == 1)
                    switch (data.Buffer[0])
                    {
                        case 0x1://Команда <пуск>
                            data.HSocket.IsBusy = true;
                            Task.Run(() => SendData(data));
                            break;
                        case 0xA://Команда <стоп>
                            data.HSocket.IsBusy = false;
                            break;
                        default:
                            break;
                    }
                AwaitRecieveData(clientId);

            }
            catch (SocketException socketException)
            {
                OnClientDisconnect(clientId);
                Debug.WriteLine(socketException.Message);
            }

        }

        /// <summary>
        /// Передача клиенту 3 байта со случайными значениями 
        /// </summary>
        /// <param name="pack"></param>
        private static void SendData(Packet pack)
        {
            var sock = pack.HSocket.Socket;
            while (sock.Connected && pack.HSocket.IsBusy)
            {
                var rnd = new Random();
                var args = new SocketAsyncEventArgs();
                byte[] buffer = new byte[3];
                rnd.NextBytes(buffer);
                args.SetBuffer(buffer, 0, buffer.Length);
                sock.Send(buffer);
                Thread.Sleep(100);
            }
        }

        #region Properties

        /// <summary>
        /// Обратный вызов при подключении клиента
        /// </summary>
        public Action<Guid> OnClientConnect { get; set; }

        /// <summary>
        /// Обратный вызов при отключении клиента
        /// </summary>
        public Action<Guid> OnClientDisconnect { get; set; }

        #endregion

        public class HandlerSocket
        {
            public HandlerSocket(Socket socket, Guid clientId)
            {
                Socket = socket;
                ClientId = clientId;
            }
            public Socket Socket { get; }
            public Guid ClientId { get; }
            public bool IsBusy { get; set; }
        }

        private class Packet
        {
            public Packet(HandlerSocket socket)
            {
                HSocket = socket;
            }
            public HandlerSocket HSocket { get; }
            public readonly byte[] Buffer = new byte[1];
        }
    }
}
