using System;
using System.Collections.Generic;
using System.Linq;
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
        private bool _IsConnectValid;

        public MainWindow()
        {
            InitializeComponent();
            _refersh();
        }

        #region Events

        private void BtnConnDisc_Click(object sender, RoutedEventArgs e)
        {
            _IsConnectValid = _connectServer();
            _refersh();
            if (_IsConnectValid)
            {
                
            }
            else
            {
                
            }
        }

        private void BtnStartStop_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region Methods

        private void _refersh()
        {
            if (!_IsConnectValid)
            {
                BtnConnDisc.Content = "Соединить";
                BtnStartStop.IsEnabled = false;
            }
            else
            {
                BtnConnDisc.Content = "Разъединить";
                BtnStartStop.IsEnabled = true;
            }
        }

        private bool _connectServer()
        {
            if (_clientTcp == null)
            {
                _clientTcp = new ClientTcp();
            }
            else
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
