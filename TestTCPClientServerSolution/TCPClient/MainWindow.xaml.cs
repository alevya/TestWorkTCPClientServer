using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

            _clientTcp.Disconnect();
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
    }
}
