using NationalInstruments.Visa;
using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DS1054Z
{
    /// <summary>
    /// Represents the waveform preamble data returned by the Rigol DS1054Z oscilloscope.
    /// Contains metadata about the waveform including format, number of points, and scaling factors.
    /// </summary>
    public struct WaveformPreamble
    {
        /// <summary>
        /// The waveform format: 0 = BYTE, 1 = WORD, 2 = ASCII.
        /// </summary>
        public int format;

        /// <summary>
        /// The acquisition type: 0 = NORMAL, 1 = PEAK, 2 = AVERAGE, 3 = HIGH_RESOLUTION.
        /// </summary>
        public int type;

        /// <summary>
        /// The number of waveform points in the data.
        /// </summary>
        public int points;

        /// <summary>
        /// The number of averages (for average mode) or 1 for other modes.
        /// </summary>
        public int count;

        /// <summary>
        /// The time increment between data points in seconds.
        /// </summary>
        public double xIncrement;

        /// <summary>
        /// The time offset from trigger in seconds.
        /// </summary>
        public double xOrigin;

        /// <summary>
        /// The reference point for X-axis calculations (typically 0).
        /// </summary>
        public double xReference;

        /// <summary>
        /// The voltage increment per LSB (least significant bit).
        /// </summary>
        public double yIncrement;

        /// <summary>
        /// The voltage offset in volts.
        /// </summary>
        public double yOrigin;

        /// <summary>
        /// The reference point for Y-axis calculations (typically 0 or 127 for byte data).
        /// </summary>
        public double yReference;

        /// <summary>
        /// The raw preamble text received from the oscilloscope.
        /// </summary>
        public string rawText;

        /// <summary>
        /// Parses a comma-separated preamble string from the Rigol DS1054Z into a WaveformPreamble structure.
        /// </summary>
        /// <param name="preamble">The comma-separated preamble string in the format: format,type,points,count,xinc,xorig,xref,yinc,yorig,yref</param>
        /// <returns>A parsed <see cref="WaveformPreamble"/> structure.</returns>
        /// <exception cref="FormatException">Thrown when the preamble string doesn't contain at least 10 comma-separated values.</exception>
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

    /// <summary>
    /// Represents a label item with text and color for display in the UI.
    /// Implements INotifyPropertyChanged to support data binding.
    /// </summary>
    public sealed class LabelItem : INotifyPropertyChanged
    {
        private string text;

        /// <summary>
        /// Gets or sets the text content of the label.
        /// </summary>
        public string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }

        /// <summary>
        /// Gets or sets the foreground brush color for the label.
        /// </summary>
        public Brush Foreground { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Main window for the DS1054Z oscilloscope viewer application.
    /// Provides real-time waveform display and control for Rigol DS1054Z oscilloscope via TCP/IP.
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly TimeSpan ThreadShutdownTimeout = TimeSpan.FromSeconds(2);
        
        // TCP/IP address of the Rigol DS1054Z oscilloscope
        private string TCPIPAddress = @"TCPIP0::192.168.1.145::inst0::INSTR";
        
        private ResourceManager ResMgr = new ResourceManager();
        private TcpipSession TCPIPSession;
        private ScpiSession SCPISession;
        
        // Background thread for continuously updating waveform display
        private Thread UpdateDisplayThread;
        private CancellationTokenSource CancellationTokenSource;
        
        // Tracks which channels are currently enabled for display
        private bool[] ChannelEnabled = new bool[4] { false, false, false, false };
        
        // Chart series objects for each channel's waveform
        private FastLineSeries[] ChannelTraces = new FastLineSeries[4];
        
        /// <summary>
        /// Gets the collection of label items for channel information display.
        /// </summary>
        public ObservableCollection<LabelItem> Labels { get; set; }
        
        // Color scheme for the four oscilloscope channels
        private SolidColorBrush[] ChannelColors = new SolidColorBrush[4]
        {
            new SolidColorBrush(Colors.Yellow),
            new SolidColorBrush(Colors.Cyan),
            new SolidColorBrush(Colors.Violet),
            new SolidColorBrush(Colors.Blue)
        };


        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// Sets up communication with the oscilloscope and initializes the chart display.
        /// </summary>
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

        /// <summary>
        /// Initializes communication with the oscilloscope via TCP/IP.
        /// Retries connection indefinitely until successful.
        /// </summary>
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

        /// <summary>
        /// Initializes the oscilloscope settings for waveform acquisition.
        /// Configures waveform format, mode, and point range, then stops the scope.
        /// </summary>
        private void InitializeScope()
        {
            SendCommand(":WAVeform:FORMat BYTE");
            SendCommand(":WAVeform:MODE NORMal");
            SendCommand(":WAVeform:STARt 1");
            
            // Query the actual available points from the device and set STOP accordingly
            int availablePoints = SCPISession.QueryWaveformPoints();
            SendCommand($":WAVeform:STOP {availablePoints}");
            
            SendCommand(":STOP");
            for (int ch = 1; ch <= 4; ch++)
            {
                SendCommand(":CHANnel" + ch.ToString() + ":DISPlay OFF");
            }
        }

        /// <summary>
        /// Sends a SCPI command to the oscilloscope and logs it to debug output.
        /// </summary>
        /// <param name="Command">The SCPI command to send.</param>
        private void SendCommand(string Command)
        {
            Debug.WriteLine(Command);
            TCPIPSession.FormattedIO.WriteLine(Command);
        }

        /// <summary>
        /// Handles the Window.Loaded event. Starts the background thread for updating waveform display.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var token = CancellationTokenSource.Token; // capture once
            UpdateDisplayThread = new Thread(() => GetDisplayWaveform(token));
            UpdateDisplayThread.Start();
        }

        /// <summary>
        /// Background thread method that continuously retrieves waveform data from enabled channels
        /// and updates the chart display. Runs until cancellation is requested.
        /// </summary>
        /// <param name="token">Cancellation token to signal thread shutdown.</param>
        private void GetDisplayWaveform(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                for (int channelNumber = 0; channelNumber < 4; channelNumber++)
                {
                    if (token.IsCancellationRequested)
                        break;

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
                        // QueryBinaryBlock returns payload only; no terminator expected since TerminationCharacterEnabled = false
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

                    if (token.IsCancellationRequested)
                        break;

                    try
                    {
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

                                Labels[channelNumber].Text = string.Format(
                                    "C{0}\nVPP {1}\nScale {2}\nTimebase {3}",
                                    ch,
                                    ToEngineeringFormat.Convert(VppResult, 3, "V"),
                                    ToEngineeringFormat.Convert(ChannelScaleResult, 3, "V"),
                                    ToEngineeringFormat.Convert(TimebaseResult, 3, "S"));
                            }
                            catch (ArgumentException ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }
                        });
                    }
                    catch (System.Threading.Tasks.TaskCanceledException)
                    {
                        // Dispatcher is shutting down, exit gracefully
                        Debug.WriteLine("Dispatcher shutdown detected, exiting GetDisplayWaveform.");
                        return;
                    }
                }

                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Handles the Window.Closing event. Stops the background thread and disposes of VISA resources.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CancellationTokenSource?.Cancel();

            if (UpdateDisplayThread != null && UpdateDisplayThread.IsAlive)
            {
                if (!UpdateDisplayThread.Join(ThreadShutdownTimeout))
                {
                    Debug.WriteLine($"Warning: UpdateDisplayThread did not exit within {ThreadShutdownTimeout.TotalSeconds} seconds.");
                    return; // avoid disposing CTS while thread may still run
                }
            }

            SCPISession?.Dispose();
            TCPIPSession?.Dispose();
            ResMgr?.Dispose();
            CancellationTokenSource?.Dispose();
        }

        /// <summary>
        /// Handles the RunStop checkbox checked event. Sends the RUN command to the oscilloscope.
        /// </summary>
        private void RunStop_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":RUN");
        }

        /// <summary>
        /// Handles the RunStop checkbox unchecked event. Sends the STOP command to the oscilloscope.
        /// </summary>
        private void RunStop_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":STOP");
        }

        /// <summary>
        /// Handles the Auto button click. Triggers the oscilloscope's auto-scale function and starts acquisition.
        /// </summary>
        private void Auto_Click(object sender, RoutedEventArgs e)
        {
            SendCommand(":AUToscale");
            RunStop.IsChecked = true;
        }

        /// <summary>
        /// Handles the Single button click. Triggers a single acquisition on the oscilloscope.
        /// </summary>
        private void Single_Click(object sender, RoutedEventArgs e)
        {
            SendCommand(":SINGle");
            RunStop.IsChecked = false;
        }

        /// <summary>
        /// Handles Channel 1 checkbox checked event. Enables Channel 1 display and measurement.
        /// </summary>
        private void Channel1_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel1:DISPlay ON");
            ChannelTraces[0].Visibility = Visibility.Visible;
            ChannelEnabled[0] = true;

            SendCommand(":MEASure:ITEM VPP,CHANnel1");
        }

        /// <summary>
        /// Handles Channel 1 checkbox unchecked event. Disables Channel 1 display.
        /// </summary>
        private void Channel1_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel1:DISPlay OFF");
            ChannelTraces[0].Visibility = Visibility.Hidden;
            ChannelEnabled[0] = false;
        }

        /// <summary>
        /// Handles Channel 2 checkbox checked event. Enables Channel 2 display and measurement.
        /// </summary>
        private void Channel2_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel2:DISPlay ON");
            ChannelTraces[1].Visibility = Visibility.Visible;
            ChannelEnabled[1] = true;

            SendCommand(":MEASure:ITEM VPP,CHANnel2");
        }

        /// <summary>
        /// Handles Channel 2 checkbox unchecked event. Disables Channel 2 display.
        /// </summary>
        private void Channel2_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel2:DISPlay OFF");
            ChannelTraces[1].Visibility = Visibility.Hidden;
            ChannelEnabled[1] = false;
        }

        /// <summary>
        /// Handles Channel 3 checkbox checked event. Enables Channel 3 display and measurement.
        /// </summary>
        private void Channel3_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel3:DISPlay ON");
            ChannelTraces[2].Visibility = Visibility.Visible;
            ChannelEnabled[2] = true;

            SendCommand(":MEASure:ITEM VPP,CHANnel3");
        }

        /// <summary>
        /// Handles Channel 3 checkbox unchecked event. Disables Channel 3 display.
        /// </summary>
        private void Channel3_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel3:DISPlay OFF");
            ChannelTraces[2].Visibility = Visibility.Hidden;
            ChannelEnabled[2] = false;
        }

        /// <summary>
        /// Handles Channel 4 checkbox checked event. Enables Channel 4 display and measurement.
        /// </summary>
        private void Channel4_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel4:DISPlay ON");
            ChannelTraces[3].Visibility = Visibility.Visible;
            ChannelEnabled[3] = true;

            SendCommand(":MEASure:ITEM VPP,CHANnel4");
        }

        /// <summary>
        /// Handles Channel 4 checkbox unchecked event. Disables Channel 4 display.
        /// </summary>
        private void Channel4_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel4:DISPlay OFF");
            ChannelTraces[3].Visibility = Visibility.Hidden;
            ChannelEnabled[3] = false;
        }
    }

    /// <summary>
    /// Value converter that converts between Visibility and boolean values for data binding.
    /// </summary>
    public class VisibilityToCheckedConverter : IValueConverter
    {
        /// <summary>
        /// Converts a Visibility value to a boolean value.
        /// </summary>
        /// <param name="value">The Visibility value to convert.</param>
        /// <param name="targetType">The target type (not used).</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture information (not used).</param>
        /// <returns>True if visibility is Visible, false otherwise.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((Visibility)value) == Visibility.Visible;
        }

        /// <summary>
        /// Converts a boolean value to a Visibility value.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The target type (not used).</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture information (not used).</param>
        /// <returns>Visible if value is true, Collapsed if false.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
