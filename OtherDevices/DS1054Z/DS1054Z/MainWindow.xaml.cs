using NationalInstruments.Visa;
using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DS1054Z
{
    public struct WaveformPreamble
    {
        public int format;
        public int type;
        public long points;
        public int count;
        public double xincrement;
        public double xorigin;
        public int xreference;
        public double yincrement;
        public int yorigin;
        public int yreference;
    }

    public class DataPoint
    {
        public int X { get; set; }
        public byte Y { get; set; }
    }

    public class ChartViewModel
    {
        public List<DataPoint> ByteSeries { get; }

        public ChartViewModel(byte[] data)
        {
            ByteSeries = ConvertBytes(data);
        }

        public List<DataPoint> ConvertBytes(byte[] bytes)
        {
            var list = new List<DataPoint>();

            for (int i = 0; i < bytes.Length; i++)
            {
                list.Add(new DataPoint { X = i, Y = bytes[i] });
            }

            return list;
        }
    }



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string DMMAddress = @"TCPIP0::192.168.1.145::inst0::INSTR";
        private ResourceManager ResMgr = new ResourceManager();
        private TcpipSession TcpipSession;
        private Thread UpdateDisplayThread;
        private bool[] ChannelEnabled = new bool[4] { false, false, false, false };
        private FastLineSeries[] ChannelTraces = new FastLineSeries[4];
        private SolidColorBrush[] ChannelColors = new SolidColorBrush[4]
        {
            new SolidColorBrush(Colors.Yellow),
            new SolidColorBrush(Colors.Cyan),
            new SolidColorBrush(Colors.Violet),
            new SolidColorBrush(Colors.Blue)
        };

        public ObservableCollection<string> LabelTexts { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            InitializeComms();
            InitializeScope();

            for (int i = 0; i < 4; i++)
            {
                byte[] buffer = Enumerable.Repeat<byte>(127, 1199).ToArray();


                ChannelTraces[i] = new FastLineSeries();
                // ItemsSource must be an enumerable of data points; use the ByteSeries list
                ChannelTraces[i].ItemsSource = new ChartViewModel(buffer).ByteSeries;
                ChannelTraces[i].XBindingPath = "X";
                ChannelTraces[i].YBindingPath = "Y";
                ChannelTraces[i].Interior = ChannelColors[i];
                ChannelTraces[i].StrokeThickness = 1;
                ChannelTraces[i].Visibility = Visibility.Hidden;
                DisplayChart.Series.Add(ChannelTraces[i]);
            }

            //ChannelTraces[0].Stroke = new SolidColorBrush(Colors.Yellow);
            //ChannelTraces[1].Stroke = new SolidColorBrush(Colors.Cyan);
            //ChannelTraces[2].Stroke = new SolidColorBrush(Colors.Violet);
            //ChannelTraces[3].Stroke = new SolidColorBrush(Colors.Blue);

            LabelTexts = new ObservableCollection<string>
            {
            "CH 1",
            "CH 2",
            "CH 3",
            "CH 4"};

            DataContext = this;
        }

        private void InitializeComms()
        {
            bool IsConnected = false;

            while (!IsConnected)
            {
                try
                {
                    TcpipSession = (TcpipSession)ResMgr.Open(DMMAddress);
                    IsConnected = true;
                }
                catch (Exception)
                {
                    IsConnected = false;
                }
            }

            TcpipSession.TerminationCharacterEnabled = true;
            TcpipSession.TimeoutMilliseconds = 20000;
            TcpipSession.Clear();
        }

        private void InitializeScope()
        {
            SendCommand(":WAVeform:FORMat BYTE");
            SendCommand(":WAVeform:MODE NORMal");
            SendCommand(":WAVeform:STARt 1");
            SendCommand(":WAVeform:STOP 1200");
            SendCommand(":STOP");
            for (int i = 0; i < 4; i++)
            {
                SendCommand(":CHANnel" + i.ToString() + ":DISPlay OFF");
            }
        }

        private void SendCommand(string Command)
        {
            Debug.WriteLine(Command);
            TcpipSession.FormattedIO.WriteLine(Command);
        }

        private byte[] GetByteData()
        {
            try
            {
                return TcpipSession.RawIO.Read(1212);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDisplayThread = new Thread(GetDisplayWaveform);
            UpdateDisplayThread.Start();
        }

        private WaveformPreamble GetWaveformPreamble()
        {
            /* Preamble string format
            int format;
            int type;
            long points;
            int count;
            double xincrement;
            double xorigin;
            int xreference;
            double yincrement;
            int yorigin;
            int yreference;*/

            WaveformPreamble preamble = new WaveformPreamble();

            SendCommand(":WAVeform:PREamble?");
            var result = TcpipSession.FormattedIO.ReadString().Split(',');

            preamble.format = Convert.ToInt32(result[0]);
            preamble.type = Convert.ToInt32(result[1]);
            preamble.points = Convert.ToInt64(result[2]);
            preamble.count = Convert.ToInt32(result[3]);
            preamble.xincrement = Convert.ToDouble(result[4]);
            preamble.xorigin = Convert.ToDouble(result[5]);
            preamble.xreference = Convert.ToInt32(result[6]);
            preamble.yincrement = Convert.ToDouble(result[7]);
            preamble.yorigin = Convert.ToInt32(result[8]);
            preamble.yreference = Convert.ToInt32(result[9]);

            return preamble;
        }

        private void GetDisplayWaveform()
        {
            double result = 0;

            WaveformPreamble preamble;
            const int headerSize = 12;

            while (true)
            {

                for (int channelNumber = 0; channelNumber < 4; channelNumber++)
                {
                    if (ChannelEnabled[channelNumber])
                    {
                        // Select the waveform source before requesting the preamble to ensure
                        // the instrument returns the preamble for the correct channel.
                        SendCommand(":WAVeform:SOURce CHANnel" + (channelNumber + 1));

                        preamble = GetWaveformPreamble();
                        SendCommand(":WAVeform:DATA?");
                        byte[] byteArray = GetByteData();

                        try
                        {
                            SendCommand(String.Format(":MEASure:ITEM? VPP,CHANnel{0}", channelNumber+1));
                            result = TcpipSession.FormattedIO.ReadDouble();
                        }
                        catch (Exception)
                        {
                            result = 0;
                        }


                        Debug.WriteLine("C1 VPP " + result);

                        this.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                var source = byteArray ?? Array.Empty<byte>();

                                // Compute available payload bytes after the header
                                int available = Math.Max(0, source.Length - headerSize);

                                // Determine bytes per sample from the preamble format
                                // Common convention: format == 0 => BYTE (1 byte/sample), format == 1 => WORD (2 bytes/sample)
                                int bytesPerPoint = (preamble.format == 0) ? 1 : 2;

                                // Compute expected bytes from the preamble if available, safely clamped to int
                                long expectedBytesLong = 0;
                                if (preamble.points > 0)
                                {
                                    expectedBytesLong = preamble.points * (long)bytesPerPoint;
                                }

                                int expectedBytes = expectedBytesLong > 0
                                    ? (int)Math.Min(expectedBytesLong, int.MaxValue)
                                    : 0;

                                int length;

                                // If the preamble provides an expected size, use the smaller of expected and available.
                                // Otherwise use the available bytes.
                                if (expectedBytes > 0)
                                    length = Math.Min(available, expectedBytes);
                                else
                                    length = available;

                                // Ignore the last byte on a successful read (instrument appends an extra terminator/checksum byte).
                                if (byteArray != null && length > 0)
                                {
                                    length = Math.Max(0, length - 1);
                                }

                                // Ensure we never allocate a negative or zero-length array unnecessarily
                                var payload = length > 0 ? new byte[length] : Array.Empty<byte>();
                                if (length > 0)
                                    Array.Copy(source, headerSize, payload, 0, length);

                                ChannelTraces[channelNumber].ItemsSource =
                                    new ChartViewModel(payload).ByteSeries;

                                LabelTexts[channelNumber] = string.Format(
                                    "CH {0} {1}",
                                    channelNumber + 1,
                                    ToEngineeringFormat.Convert(result, 3, "V", true)
                                );
                            }
                            catch (ArgumentException ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }

                        });
                    }
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void RunStop_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":RUN");
        }

        private void RunStop_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":STOP");
        }

        private void Auto_Click(object sender, RoutedEventArgs e)
        {
            SendCommand(":AUToscale");
            RunStop.IsChecked = true;
        }

        private void Single_Click(object sender, RoutedEventArgs e)
        {
            SendCommand(":SINGle");
            RunStop.IsChecked = false;
        }

        private void Channel1_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel1:DISPlay ON");
            ChannelTraces[0].Visibility = Visibility.Visible;
            ChannelEnabled[0] = true;

            SendCommand(":MEASure:ITEM VPP,CHANnel1");
        }

        private void Channel1_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel1:DISPlay OFF");
            ChannelTraces[0].Visibility = Visibility.Hidden;
            ChannelEnabled[0] = false;
        }

        private void Channel2_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel2:DISPlay ON");
            ChannelTraces[1].Visibility = Visibility.Visible;
            ChannelEnabled[1] = true;

            SendCommand(":MEASure:ITEM VPP,CHANnel2");
        }

        private void Channel2_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel2:DISPlay OFF");
            ChannelTraces[1].Visibility = Visibility.Hidden;
            ChannelEnabled[1] = false;
        }

        private void Channel3_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel3:DISPlay ON");
            ChannelTraces[2].Visibility = Visibility.Visible;
            ChannelEnabled[2] = true;

            SendCommand(":MEASure:ITEM VPP,CHANnel3");
        }

        private void Channel3_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel3:DISPlay OFF");
            ChannelTraces[2].Visibility = Visibility.Hidden;
            ChannelEnabled[2] = false;
        }

        private void Channel4_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel4:DISPlay ON");
            ChannelTraces[3].Visibility = Visibility.Visible;
            ChannelEnabled[3] = true;

            SendCommand(":MEASure:ITEM VPP,CHANnel4");
        }

        private void Channel4_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel4:DISPlay OFF");
            ChannelTraces[3].Visibility = Visibility.Hidden;
            ChannelEnabled[3] = false;
        }
    }

    public class VisibilityToCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((Visibility)value) == Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
