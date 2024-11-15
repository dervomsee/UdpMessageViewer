using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        private CancellationTokenSource cancellationTokenSource;
        private UdpClient listenClient;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async Task SimplestReceiverAsync(CancellationToken cancellationToken)
        {
            TextBoxAppendMessage("Receiver started, listening on port " + listenPort + ".\n--------------------------------------------------------\n\n", "");

            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
            listenClient = new UdpClient(listenEndPoint);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    //wait for UDP data
                    UdpReceiveResult result = await listenClient.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(result.Buffer);

                    //get the sender's IP
                    string senderIP = result.RemoteEndPoint.Address.ToString();

                    //display the message
                    TextBoxAppendMessage(message, senderIP);
                }
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode != 10060)
                {
                    TextBoxAppendMessage("a more serious error " + ex.ErrorCode, "");
                }
                else
                {
                    TextBoxAppendMessage("expected timeout error", "");
                }
            }
            catch (ObjectDisposedException)
            {
                // Handle the case when the UdpClient is disposed
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //stop listenClient and listenThread and cleanup
            StopReceiver();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource == null)
            {
                if (int.TryParse(PortNumber.Text, out listenPort) && listenPort > 0 && listenPort <= 65535)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    ConnectButton.Content = "Disconnect";
                    await Task.Run(() => SimplestReceiverAsync(cancellationTokenSource.Token));                    
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
                StopReceiver();
                ConnectButton.Content = "Connect";
            }
        }

        private void StopReceiver()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                listenClient.Close();
                cancellationTokenSource = null;
                Dispatcher.BeginInvoke((Action)(() => UdpMessageTextbox.AppendText("\n--------------------------------------------------------\nReceiver stopped.\n--------------------------------------------------------\n")));
            }
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

        private void TextBoxAppendMessage(string s, string senderIP)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                string filterIP = IpFilter.Text;

                //check is filterIP is a substring senderIP
                if (!string.IsNullOrEmpty(filterIP) && !senderIP.Contains(filterIP))
                {
                    return;
                }

                //display sender's IP if available
                if (!string.IsNullOrEmpty(senderIP))
                {
                    UdpMessageTextbox.AppendText(senderIP + ": ");
                }

                //append the message
                UdpMessageTextbox.AppendText(s);
                UdpMessageTextbox.ScrollToEnd();
            }));
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            UdpMessageTextbox.Clear();
        }
    }
}