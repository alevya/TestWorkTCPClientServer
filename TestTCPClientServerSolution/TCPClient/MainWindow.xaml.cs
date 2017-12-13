using System;
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

        private readonly string _serverName; //TODO get from Configuration
        private readonly string _portNum;      //TODO get from Configuration

        public MainWindow()
        {
            InitializeComponent();
            _refersh();
            _serverName = "localhost";
            _portNum = "11111";
        }

        #region Events

        private void BtnConnDisc_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnectValid == false)
            {
                _isConnectValid = _connectServer();
            }
            else
            {
                _disconnectServer();
                _isConnectValid = false;
            }
            _refersh();
        }

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
            _refersh();
            _sendMessage(data);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _disconnectServer();
        }

        private void OnReceiveData(byte[] bytes, int size)
        {
            ColorWindow = new Color { R = bytes[0], G = bytes[1], B = bytes[2], A = 255};
            Dispatcher.BeginInvoke(new ThreadStart(delegate
                                                   {
                                                       Background = new SolidColorBrush(ColorWindow);
                                                   }));
            
        }

        #endregion

        #region Methods

        private void _refersh()
        {
            if (!_isConnectValid)
            {
                BtnConnDisc.Content = "Соединить";
                BtnStartStop.IsEnabled = false;
            }
            else
            {
                BtnConnDisc.Content = "Разъединить";
                BtnStartStop.IsEnabled = true;
            }

            BtnStartStop.Content = !_isStart ? "Пуск" : "Стоп";
        }


        private bool _connectServer()
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

            var ipAddr = _resolveIpAddress();
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
                Console.WriteLine(e);
            }

            return false;
        }

        

        private void _disconnectServer()
        {
            if(_clientTcp == null) return;

            if (_clientTcp.OnReceiveData != null)
                _clientTcp.OnReceiveData -= OnReceiveData;
            _clientTcp.Disconnect();
            _clientTcp = null;
        }

        private IPAddress _resolveIpAddress()
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
                Console.WriteLine(e);

            }
            return IPAddress.Parse(addrServer);
        }

        private void _sendMessage(byte[] data)
        {
            if(_clientTcp.IsConnected)
                _clientTcp.SendData(data);

        }

        #endregion

        public Color ColorWindow { get; private set; }
    }
}
