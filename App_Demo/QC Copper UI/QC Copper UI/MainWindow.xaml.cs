using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;

namespace QC_Copper_UI
{
    public partial class MainWindow : Window
    {
        private Thread Serial_Thread;
        private volatile bool stopThread = false;
        private SerialPort _serialPort1;
        public static bool isSerialPort1Initialized = false;
        private const int MaxLogItems = 1000;
        private const string LogFilePath = "log.txt"; // Path to the log file

        public MainWindow()
        {
            InitializeComponent();
            Serial_Thread = new Thread(new ThreadStart(Read_Serial));
            Serial_Thread.Start();
        }

        private void Read_Serial()
        {
            while (!stopThread)
            {
                try
                {
                    if (!isSerialPort1Initialized)
                    {
                        if (_serialPort1 != null && _serialPort1.IsOpen)
                        {
                            _serialPort1.Close();
                        }
                        try
                        {
                            _serialPort1 = new SerialPort("COM13", 9600, Parity.None, 8, StopBits.One);
                            _serialPort1.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
                            _serialPort1.Open();
                            isSerialPort1Initialized = true;
                        }
                        catch
                        {
                            isSerialPort1Initialized = false;
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch
                {
                    isSerialPort1Initialized = false;
                }
                Thread.Sleep(1);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                string item = sp.ReadLine();
                string log = $"{DateTime.Now} --> Khối Lượng Tịnh: {item.Trim()} (Kg)";
                Dispatcher.Invoke(() =>
                {
                    Value.Text = item;
                    Log.Items.Add(log);
                    if (Log.Items.Count > MaxLogItems)
                    {
                        Log.Items.RemoveAt(0);
                    }
                    Log.ScrollIntoView(Log.Items[Log.Items.Count - 1]);
                });
                // Append log to file
                File.AppendAllText(LogFilePath, log + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Handle the exception as needed
                isSerialPort1Initialized = false;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
           stopThread = true;
            if (_serialPort1 != null && _serialPort1.IsOpen)
            {
                _serialPort1.Close();
            }
            Serial_Thread.Join();
            base.OnClosing(e);
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
