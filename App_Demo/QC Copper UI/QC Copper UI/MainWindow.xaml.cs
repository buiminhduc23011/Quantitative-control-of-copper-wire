using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Windows.Threading;
using System.Windows.Media;


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
        private string _com;
        private int _minstock;
        private DispatcherTimer _timer;
        private bool isflat = false;
        private bool _isRedBackground = false;
        private string log;
        public MainWindow()
        {
            InitializeComponent();
            Init();
            Serial_Thread = new Thread(new ThreadStart(Read_Serial));
            Serial_Thread.Start();
            InitializeTimer();


        }
        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); // 1 giây
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateUI();
            });
        }

        private void UpdateUI()
        {
            if (isflat)
            {
                if (_isRedBackground)
                {
                    Grid_Value.Background = new SolidColorBrush(Colors.White);
                }
                else
                {
                    Grid_Value.Background = new SolidColorBrush(Colors.Red);
                }
                _isRedBackground = !_isRedBackground;
            }
            else
            {
                _isRedBackground = false;
                Grid_Value.Background = new SolidColorBrush(Colors.White);
            }
        }
        public void Init()
        {
            string filePath = "scnn.json";
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                JObject jsonObject = JObject.Parse(jsonContent);
                _com = (string)jsonObject["COM"];
                _minstock = (int)jsonObject["Minstock"];
                txb_COM.Text = _com;
                Minstock.Value = _minstock;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
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
                            _serialPort1 = new SerialPort(_com, 9600, Parity.None, 8, StopBits.One);
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
                
                try
                {
                    double _kg = 0;
                    _kg = double.Parse(item.Trim()) *1000;
                    if (_kg < _minstock) isflat = true;
                    else isflat = false;
                }

                catch
                {

                }
                Dispatcher.Invoke(() =>
                {
                    
                    if (isflat)
                    {
                        log = $"{DateTime.Now} --> Khối Lượng Tịnh: {item.Trim()} (Kg) --> Số lượng đồng còn lại thấp hơn cài đặt";
                    }
                    else
                    {
                        log = $"{DateTime.Now} --> Khối Lượng Tịnh: {item.Trim()} (Kg)";
                    }    
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
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            object data_Setting = new
            {
                COM = txb_COM.Text,
                Minstock = Minstock.Value,
            };
            string json = System.Text.Json.JsonSerializer.Serialize(data_Setting);
            File.WriteAllText("scnn.json", json);
            MessageBox.Show("Đã Lưu Thành Công");
        }
        private void Tare_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
