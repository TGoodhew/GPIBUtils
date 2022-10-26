using NationalInstruments.Visa;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;

namespace DS1054Z
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string DMMAddress = @"TCPIP0::192.168.1.146::inst0::INSTR";
        private ResourceManager ResMgr = new ResourceManager();
        private TcpipSession TcpipSession;
        private Thread UpdateDisplayThread;

        public MainWindow()
        {
            InitializeComponent();
            InitializeComms();
            InitializeScope();
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
            SendCommand(":WAVeform:SOURce CHANnel1");
            SendCommand(":WAVeform:FORMat BYTE");
            SendCommand(":WAVeform:MODE NORMal");
            SendCommand(":WAVeform:STARt 1");
            SendCommand(":WAVeform:STOP 1200");
        }

        private void SendCommand(string Command)
        {
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

        private void GetDisplayWaveform()
        {
            while (true)
            {
                SendCommand(":WAVeform:SOURce CHANnel1");
                SendCommand(":WAVeform:DATA?");
                byte[] byteArray = GetByteData();

                var x = Enumerable.Range(0, 1199).Select(i => i).ToArray();

                this.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        plotter.PlotHeight = 255;
                        plotter.PlotOriginY = 0;
                        plotter.PlotWidth = 1199;

                        linegraph.Plot(x, byteArray.Skip(12).Take(byteArray.Length - 13).ToArray());
                    }
                    catch (System.ArgumentException ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }

                });
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
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
