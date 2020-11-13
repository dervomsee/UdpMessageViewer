using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace Udp_Message_Viewer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int listenPort = 34200;
        private Thread listenThread;
        private UdpClient listenClient;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SimplestReceiver()
        {
            TextBoxAppendMessage("Receiver started, listening on port " + listenPort + ".\n--------------------------------------------------------\n\n");

            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
            listenClient = new UdpClient(listenEndPoint);

            while (true)
            {
                try
                {
                    byte[] data = listenClient.Receive(ref listenEndPoint);
                    string message = Encoding.UTF8.GetString(data);
                    TextBoxAppendMessage(message);
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode != 10060)
                    {
                        TextBoxAppendMessage("a more serious error " + ex.ErrorCode);
                    }
                    else
                    {
                        TextBoxAppendMessage("expected timeout error");
                    }
                }

                Thread.Sleep(10); // tune for your situation, can usually be omitted
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //stop listenClient and listenThread and cleanup
            if (listenThread != null)
            {
                StopThread();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (listenThread == null)
            {
                if (int.TryParse(PortNumber.Text, out listenPort) && listenPort > 0 && listenPort <= 65535)
                {
                    listenThread = new Thread(new ThreadStart(SimplestReceiver));
                    listenThread.Start();
                    ConnectButton.Content = "Disconnect";
                }
                else
                {
                    string message = "The entered port number " + PortNumber.Text + " is invalid.";
                    string caption = "Invalid Port";
                    MessageBoxResult result = MessageBox.Show(message,
                                              caption,
                                              MessageBoxButton.OK,
                                              MessageBoxImage.Error);
                }

            }
            else
            {
                StopThread();
                ConnectButton.Content = "Connect";
            }

        }

        private void StopThread()
        {
            listenClient.Close();
            listenThread.Abort();
            listenThread.Join(5000);
            listenThread = null;
            Dispatcher.BeginInvoke((Action)(() => UdpMessageTextbox.AppendText("\n--------------------------------------------------------\nReceiver stopped.\n--------------------------------------------------------\n")));
        }

        private static readonly Regex _regex = new Regex("[^0-9]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        private void PortNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void TextBoxAppendMessage(string s)
        {
            Dispatcher.BeginInvoke((Action)(() =>
                  {
                      UdpMessageTextbox.AppendText(s);
                      UdpMessageTextbox.ScrollToEnd();
                  })
                );
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            UdpMessageTextbox.Clear();
        }
    }




}
