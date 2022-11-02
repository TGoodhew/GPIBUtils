﻿using InteractiveDataDisplay.WPF;
using NationalInstruments.Visa;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Data;

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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string DMMAddress = @"TCPIP0::192.168.1.146::inst0::INSTR";
        private ResourceManager ResMgr = new ResourceManager();
        private TcpipSession TcpipSession;
        private Thread UpdateDisplayThread;
        private bool[] ChannelEnabled = new bool[4] { false, false, false, false };
        private LineGraph[] ChannelTraces = new LineGraph[4];

        public MainWindow()
        {
            InitializeComponent();
            InitializeComms();
            InitializeScope();

            for (int i = 0; i < 4; i++)
            {
                ChannelTraces[i] = new LineGraph();
                //traces.Children.Add(ChannelTraces[i]);
            }
        }

        private void InitializeComms()
        {
            TcpipSession = (TcpipSession)ResMgr.Open(DMMAddress);
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
            return TcpipSession.RawIO.Read(1212);
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
            WaveformPreamble preamble;
            while (true)
            {
                for (int channelNumber = 0; channelNumber < 4; channelNumber++)
                {
                    if (ChannelEnabled[channelNumber])
                    {
                        preamble = GetWaveformPreamble();
                        SendCommand(":WAVeform:SOURce CHANnel" + (channelNumber+1));
                        SendCommand(":WAVeform:DATA?");
                        byte[] byteArray = GetByteData();

                        //BUG: X axis range not tracking actual values
                        var x = Enumerable.Range(0, 1199).Select(i => preamble.xorigin + (i * preamble.xincrement)).ToArray();

                        this.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                plotter.PlotHeight = 255;
                                plotter.PlotOriginY = 0;
                                plotter.PlotOriginX = x[0];
                                plotter.PlotWidth = x[1198] * 2;

                                ChannelTraces[channelNumber].Plot(x, byteArray.Skip(12).Take(byteArray.Length - 13).ToArray());
                            }
                            catch (System.ArgumentException ex)
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

        //TODO: Turn these into a group of buttons
        //TODO: Use visibility rather than adding and subtracting the traces
        private void Channel1_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel1:DISPlay ON");
            traces.Children.Add(ChannelTraces[0]);
            ChannelEnabled[0] = true;
        }

        private void Channel1_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel1:DISPlay OFF");
            traces.Children.Remove(ChannelTraces[0]);
            ChannelEnabled[0] = false;
        }

        private void Channel2_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel2:DISPlay ON");
            traces.Children.Add(ChannelTraces[1]);
            ChannelEnabled[1] = true;
        }

        private void Channel2_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel2:DISPlay OFF");
            traces.Children.Remove(ChannelTraces[1]);
            ChannelEnabled[1] = false;
        }

        private void Channel3_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel3:DISPlay ON"); 
            traces.Children.Add(ChannelTraces[2]);
            ChannelEnabled[2] = true;
        }

        private void Channel3_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel3:DISPlay OFF");
            traces.Children.Remove(ChannelTraces[2]);
            ChannelEnabled[2] = false;
        }

        private void Channel4_Checked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel4:DISPlay ON"); 
            traces.Children.Add(ChannelTraces[3]);
            ChannelEnabled[3] = true;
        }

        private void Channel4_Unchecked(object sender, RoutedEventArgs e)
        {
            SendCommand(":CHANnel4:DISPlay OFF");
            traces.Children.Remove(ChannelTraces[3]);
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
