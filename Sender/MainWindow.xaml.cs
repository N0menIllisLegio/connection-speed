using System;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using Common;

namespace Sender
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Random rand = new Random();
        private int remotePort;
        private IPAddress remoteIp;
        private IPEndPoint remoteEP;

        private int packetsCount;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем конечную точку отправки
                remotePort = Convert.ToInt32(txtboxPort.Text);
                remoteIp = IPAddress.Parse(txtboxIP.Text);
                remoteEP = new IPEndPoint(remoteIp, remotePort);

                SendPacket();
                SendCount();
                SendPacketTCP();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void SendPacket()
        {
            byte[] packet = new byte[CommonValues.PACKET_LENGTH];
            

            packetsCount = int.Parse(txtboxCount.Text);

            // Отправляем тестовые пакеты по Udp
            using (Socket socket = new Socket(remoteIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp))
            {
                for (int i = 0; i < packetsCount; i++)
                {
                    rand.NextBytes(packet);
                    socket.SendTo(packet, remoteEP);
                }
                socket.Shutdown(SocketShutdown.Both);
            }
        }

        private void SendPacketTCP()
        {
            byte[] packet = new byte[CommonValues.PACKET_LENGTH];
            

            Console.WriteLine(packet);

            packetsCount = int.Parse(txtboxCount.Text);

            // Отправляем тестовые пакеты по Tcp
            using (Socket socket = new Socket(remoteIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(remoteEP);

                for (int i = 0; i < packetsCount; i++)
                {
                    rand.NextBytes(packet);
                    socket.Send(packet);    
                }

                socket.Send(BitConverter.GetBytes(1));

                socket.Shutdown(SocketShutdown.Both);
            }
        }

        private void SendCount()
        {

            // Отправляе число пакетов по Tcp
            using (Socket socket = new Socket(remoteIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(remoteEP);

                socket.Send(BitConverter.GetBytes(packetsCount));

                socket.Shutdown(SocketShutdown.Both);
            }
        }
    }
}
