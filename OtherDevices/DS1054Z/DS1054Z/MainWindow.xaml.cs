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
        public int points;
        public int count;
        public double xIncrement;
        public double xOrigin;
        public double xReference;
        public double yIncrement;
        public double yOrigin;
        public double yReference;
        public string rawText;

        public static WaveformPreamble Parse(string preamble)
        {
            // Rigol DS1054Z: usually 10 comma-separated fields
            // format,type,points,count,xinc,xorig,xref,yinc,yorig,yref
            var parts = preamble.Split(new[] { ',', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 10)
                throw new FormatException("Unexpected waveform preamble format: " + preamble);

            return new WaveformPreamble
            {
                format = int.Parse(parts[0]),
                type = int.Parse(parts[1]),
                points = int.Parse(parts[2]),
                count = int.Parse(parts[3]),
                xIncrement = double.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture),
                xOrigin = double.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture),
                xReference = double.Parse(parts[6], System.Globalization.CultureInfo.InvariantCulture),
                yIncrement = double.Parse(parts[7], System.Globalization.CultureInfo.InvariantCulture),
                yOrigin = double.Parse(parts[8], System.Globalization.CultureInfo.InvariantCulture),
                yReference = double.Parse(parts[9], System.Globalization.CultureInfo.InvariantCulture),
                rawText = preamble
            };
        }
    }

    public sealed class LabelItem
    {
        public string Text { get; set; }
        public Brush Foreground { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly TimeSpan ThreadShutdownTimeout = TimeSpan.FromSeconds(2);
        private string TCPIPAddress = @"TCPIP0::192.168.1.145::inst0::INSTR";
        private ResourceManager ResMgr = new ResourceManager();
        private TcpipSession TCPIPSession;
        private ScpiSession SCPISession;
        private Thread UpdateDisplayThread;
        private CancellationTokenSource CancellationTokenSource;
        private bool[] ChannelEnabled = new bool[4] { false, false, false, false };
        private FastLineSeries[] ChannelTraces = new FastLineSeries[4];
        public ObservableCollection<LabelItem> Labels { get; set; }
        private SolidColorBrush[] ChannelColors = new SolidColorBrush[4]
        {
            new SolidColorBrush(Colors.Yellow),
            new SolidColorBrush(Colors.Cyan),
            new SolidColorBrush(Colors.Violet),
            new SolidColorBrush(Colors.Blue)
        };


        public MainWindow()
        {
            InitializeComponent();
            CancellationTokenSource = new CancellationTokenSource();
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

            Labels = new ObservableCollection<LabelItem>
            {
                new LabelItem { Text = "CH 1", Foreground = ChannelColors[0] },
                new LabelItem { Text = "CH 2", Foreground = ChannelColors[1] },
                new LabelItem { Text = "CH 3", Foreground = ChannelColors[2] },
                new LabelItem { Text = "CH 4", Foreground = ChannelColors[3] }
            };

            DataContext = this;
        }

        private void InitializeComms()
        {
            bool IsConnected = false;

            while (!IsConnected)
            {
                try
                {
                    TCPIPSession = (TcpipSession)ResMgr.Open(TCPIPAddress);
                    SCPISession = new ScpiSession(TCPIPSession);
                    IsConnected = true;
                }
                catch (Exception)
                {
                    IsConnected = false;
                }
            }

            TCPIPSession.TerminationCharacterEnabled = false; // avoid truncation/timeouts on binary reads
            TCPIPSession.TimeoutMilliseconds = 20000;
            TCPIPSession.Clear();
        }

        private void InitializeScope()
        {
            SendCommand(":WAVeform:FORMat BYTE");
            SendCommand(":WAVeform:MODE NORMal");
            SendCommand(":WAVeform:STARt 1");
            SendCommand(":WAVeform:STOP 1200");
            SendCommand(":STOP");
            for (int ch = 1; ch <= 4; ch++)
            {
                SendCommand(":CHANnel" + ch.ToString() + ":DISPlay OFF");
            }
        }

        private void SendCommand(string Command)
        {
            Debug.WriteLine(Command);
            TCPIPSession.FormattedIO.WriteLine(Command);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDisplayThread = new Thread(GetDisplayWaveform);
            UpdateDisplayThread.Start();
        }

        private void GetDisplayWaveform()
        {
            while (!CancellationTokenSource.Token.IsCancellationRequested)
            {
                for (int channelNumber = 0; channelNumber < 4; channelNumber++)
                {
                    if (!ChannelEnabled[channelNumber])
                        continue;

                    int ch = channelNumber + 1;

                    WaveformPreamble preamble;
                    byte[] payload;
                    double VppResult = 0;
                    double ChannelScaleResult = 0;
                    double TimebaseResult = 0;

                    try
                    {
                        preamble = SCPISession.QueryWaveformPreamble(ch);
                        // Consume the terminator to keep the stream aligned
                        payload = SCPISession.QueryBinaryBlock(":WAVeform:DATA?", false);

                        VppResult = SCPISession.QueryVpp(ch);
                        ChannelScaleResult = SCPISession.QueryChannelScale(ch);
                        TimebaseResult = SCPISession.QueryTimebaseScale();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("SCPI error in GetDisplayWaveform: " + ex.Message);
                        continue;
                    }

                    if (payload == null)
                    {
                        Debug.WriteLine("Waveform frame truncated or malformed — skipping.");
                        continue;
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var source = payload;
                            int bytesPerPoint = (preamble.format == 0) ? 1 : 2;

                            long expectedBytesLong = (preamble.points > 0)
                                ? preamble.points * (long)bytesPerPoint
                                : 0;

                            int expectedBytes = expectedBytesLong > 0
                                ? (int)Math.Min(expectedBytesLong, int.MaxValue)
                                : source.Length;

                            int length = Math.Min(source.Length, expectedBytes);

                            var data = (length > 0 && length == source.Length)
                                ? source
                                : (length > 0 ? source.Take(length).ToArray() : Array.Empty<byte>());

                            ChannelTraces[channelNumber].ItemsSource =
                                new ChartViewModel(data).ByteSeries;

                            Labels[channelNumber] = new LabelItem
                            {
                                Text = string.Format(
                                    "C{0}\nVPP {1}\nScale {2}\nTimebase {3}",
                                    ch,
                                    ToEngineeringFormat.Convert(VppResult, 3, "V"),
                                    ToEngineeringFormat.Convert(ChannelScaleResult, 3, "V"),
                                    ToEngineeringFormat.Convert(TimebaseResult, 3, "S")),
                                Foreground = ChannelColors[channelNumber]
                            };
                        }
                        catch (ArgumentException ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    });
                }

                Thread.Sleep(10);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Signal the thread to stop
            CancellationTokenSource?.Cancel();

            // Wait for the thread to finish
            if (UpdateDisplayThread != null && UpdateDisplayThread.IsAlive)
            {
                if (!UpdateDisplayThread.Join((int)ThreadShutdownTimeout.TotalMilliseconds))
                {
                    Debug.WriteLine($"Warning: UpdateDisplayThread did not exit within {ThreadShutdownTimeout.TotalSeconds} seconds timeout. Thread may still be running and could cause resource leaks.");
                }
            }

            // Dispose resources
            SCPISession?.Dispose();
            TCPIPSession?.Dispose();
            ResMgr?.Dispose();
            CancellationTokenSource?.Dispose();
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
