using System;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using Common;

namespace Receiver
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int localPort = 11000;
        private IPAddress localIp;
        private IPEndPoint localEP;

        private DateTime timeStart;
        private DateTime timeEnd;

        private DateTime timeStartTcp;
        private DateTime timeEndTcp;
        private int packetIndexTcp;

        private int packetsCount;
        private int packetIndex;
        private bool isAllPackets;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetLocalEP();
        }

        private void receive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetLocalEP();

                Thread thread = new Thread(ReceivePackets);
                thread.Start();
                ReceiveTime();             
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SetLocalEP()
        {
            try
            {
                // Установка конечной точки для принятия пакетов
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                localIp = Dns.GetHostAddresses(Dns.GetHostName())
                    .Where(x => x.AddressFamily == AddressFamily.InterNetwork).Last();
                localPort = int.Parse(txtboxPort.Text);
                localEP = new IPEndPoint(localIp, localPort);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            txtblockIP.Text = localIp.ToString();
        }

        private void ReceivePackets()
        {
            isAllPackets = false;
            packetIndex = 0;

            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0); // Точка отправителя

            byte[] buffer = new byte[CommonValues.PACKET_LENGTH];

            // Создание Udp сокета для принятия пакетов
            using (Socket socket = new Socket(localIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(localEP);

                // Пока отправитель не выслал все пакеты
                while (!isAllPackets || socket.Available != 0)
                {
                    if (socket.Available != 0)
                    {
                        if (packetIndex == 0)
                        {
                            timeStart = DateTime.Now; // Засекаем время перед первым пакетом
                        }

                        socket.ReceiveFrom(buffer, ref remoteEP);
                        packetIndex++;
                        timeEnd = DateTime.Now; // И после принятия каждого
                    }
                }

                // Закрываем соединение
                socket.Shutdown(SocketShutdown.Both);
                isAllPackets = false;
            }
        }

        private void ReceiveTime()
        {
            byte[] buffer = new byte[sizeof(int)];
            byte[] buffer_v2 = new byte[CommonValues.PACKET_LENGTH];

            packetIndexTcp = 0;
            int checkTranssmission = 0;

            try
            {
                // По Tcp принимаем число пакетов
                using (Socket listener = new Socket(localIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(localEP);
                    listener.Listen(2);
                    using (Socket socket = listener.Accept())
                    {
                        socket.Receive(buffer);
                        packetsCount = BitConverter.ToInt32(buffer, 0);
                        socket.Shutdown(SocketShutdown.Both);
                    }
                    isAllPackets = true; // Если пришло число, значит все пакеты отправленны

                    while (isAllPackets) { }

                    using (Socket socket = listener.Accept())
                    {
                        while (checkTranssmission != 1 || socket.Available != 0)
                        {
                            if (packetIndexTcp == 0)
                            {
                                timeStartTcp = DateTime.Now; // Засекаем время перед первым пакетом
                            }

                            socket.Receive(buffer_v2);
                            checkTranssmission = BitConverter.ToInt32(buffer_v2, 0);

                            packetIndexTcp++;
                            timeEndTcp = DateTime.Now; // И после принятия каждого
                        }

                        packetIndexTcp--;
                        socket.Shutdown(SocketShutdown.Both);      
                    }

                    ShowSpecifications();
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ShowSpecifications()
        {
            TimeSpan delta = timeEnd.Subtract(timeStart); // Время отправки всех пакетов
            double speed = TimeSpan.FromSeconds(1).Ticks * packetsCount / delta.Ticks; // Скорость
            delta = timeEndTcp.Subtract(timeStartTcp);
            double speedTcp = TimeSpan.FromSeconds(1).Ticks * packetsCount / delta.Ticks;

            double percent = (double) packetIndex / packetsCount * 100; // Процент принятых пакетов
            double percentTcp = (double) packetIndexTcp / packetsCount * 100;

            txtblockPercent.Text = "Percent UDP: " + Math.Round(percent, 5) + " %";
            txtblockSpeed.Text = "Speed UDP: " + speed + " packets/sec";

            txtblockPercentTCP.Text = "Percent TCP: " + Math.Round(percentTcp, 5) + " %";
            txtblockSpeedTCP.Text = "Speed TCP: " + speedTcp + " packets/sec";
        }
    }
}