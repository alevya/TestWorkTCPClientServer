using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace TCPClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ClientTcp _clientTcp;
        private bool _isConnectValid;
        private bool _isStart;

        private readonly string _serverName; 
        private readonly string _portNum;

        public MainWindow()
        {
            InitializeComponent();
            Refersh();

            var appSettings = ConfigurationManager.AppSettings;
            _serverName = appSettings.Get("server") ?? "localhost";
            _portNum = appSettings.Get("port") ?? "11111";
        }

        #region Events

        /// <summary>
        /// Обработчик нажатия кнопки [Соединить/Разъединить]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnConnDisc_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnectValid == false)
            {
                _isConnectValid = ConnectServer();
            }
            else
            {
                DisconnectServer();
                _isConnectValid = false;
                _isStart = false;
            }
            Refersh();
        }

        /// <summary>
        /// Обработчик нажатия кнопки [Пуск/Стоп]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnStartStop_Click(object sender, RoutedEventArgs e)
        {
            byte[] data;
            if (_isStart == false)
            {
                data = new byte[]{0x1};
                _isStart = true;
            }
            else
            {
                data = new byte[] { 0xA };
                _isStart = false;
            }
            Refersh();
            SendMessage(data);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DisconnectServer();
        }

        /// <summary>
        /// Получение данных и перекрашивание окна
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="size"></param>
        private void OnReceiveData(byte[] bytes, int size)
        {
            var color = new Color { R = bytes[0], G = bytes[1], B = bytes[2], A = 255};
            Dispatcher.BeginInvoke(new ThreadStart(delegate
                                                   {
                                                       Background = new SolidColorBrush(color);
                                                   }));
            
        }

        #endregion

        #region Methods

        private void Refersh()
        {
            if (!_isConnectValid)
            {
                BtnConnDisc.Content = "Соединить";
                BtnStartStop.Content = "Стоп";
                BtnStartStop.IsEnabled = false;
            }
            else
            {
                BtnConnDisc.Content = "Разъединить";
                BtnStartStop.Content = !_isStart ? "Пуск" : "Стоп";
                BtnStartStop.IsEnabled = true;
            }
        }

        /// <summary>
        /// Установление соединения с сервером
        /// </summary>
        /// <returns></returns>
        private bool ConnectServer()
        {
            if (_clientTcp == null)
            {
                _clientTcp = new ClientTcp();
                _clientTcp.OnReceiveData += OnReceiveData;
            }
            else
            {
                if(_clientTcp.IsConnected)
                    return true;
            }

            var ipAddr = ResolveIpAddress();
            if (ipAddr == null) return false;

            if (!int.TryParse(_portNum, out int port))
            {
                port = 11111;
            }

            try
            {
                _clientTcp.Connect(ipAddr, port);
                return _clientTcp.IsConnected;
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        /// <summary>
        /// Отключение от сервера
        /// </summary>
        private void DisconnectServer()
        {
            if(_clientTcp == null) return;

            if (_clientTcp.OnReceiveData != null)
                _clientTcp.OnReceiveData -= OnReceiveData;
            _clientTcp.Disconnect();
            _clientTcp = null;
        }

        /// <summary>
        /// Определение IP-адреса сервера
        /// </summary>
        /// <returns></returns>
        private IPAddress ResolveIpAddress()
        {
            var addrServer = _serverName;
            if (string.IsNullOrEmpty(addrServer)) return null;

            var addrSplit = addrServer.Split('.');
            if (addrSplit.Length == 4) return IPAddress.Parse(addrServer);

            try
            {
                var ipHostEntry = Dns.GetHostEntry(addrServer);
                foreach (var ipAddr in ipHostEntry.AddressList)
                {
                    if (ipAddr.AddressFamily != AddressFamily.InterNetwork) continue;

                    addrServer = ipAddr.ToString();
                    break;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return IPAddress.Parse(addrServer);
        }

        /// <summary>
        /// Отправка команды серверу
        /// </summary>
        /// <param name="data"></param>
        private void SendMessage(byte[] data)
        {
            if(_clientTcp.IsConnected)
                _clientTcp.SendData(data);

        }

        #endregion
    }
}
