using Ivi.Visa.FormattedIO;
using NationalInstruments.Visa;
using Ivi.Visa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DM3058
{
    enum Mode {DCV, ACV, DCI, ACI, OHM };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static RoutedCommand SetModeCommand = new RoutedCommand();

        private string DMMAddress = @"TCPIP0::192.168.1.211::inst0::INSTR";
        private ResourceManager ResMgr = new ResourceManager();
        private DispatcherTimer ReadTimer;
        private Mode CurrentMode;
        private string CurrentCommand;
        private TcpipSession TcpipSession;

        public MainWindow()
        {
            InitializeComponent();

            CommandBinding SetModeCommandBinding = new CommandBinding(SetModeCommand, ExecutedSetModeCommand, CanExecuteSetModeCommand);
            this.CommandBindings.Add(SetModeCommandBinding);

            InitializeDMM();
            SetMode("DCV");

            InitializeTimer();
        }

        private void InitializeTimer()
        {
            ReadTimer = new DispatcherTimer();
            ReadTimer.Interval = TimeSpan.FromSeconds(1);
            ReadTimer.Tick += Timer_Tick;
        }

        private void InitializeDMM()
        {
            try
            {
                TcpipSession = (TcpipSession)ResMgr.Open(DMMAddress);
                TcpipSession.TerminationCharacterEnabled = true;
                TcpipSession.TimeoutMilliseconds = 20000;
                TcpipSession.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to connect to DMM at {DMMAddress}\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    "Please check:\n" +
                    "- Device is powered on and connected to network\n" +
                    "- IP address is correct\n" +
                    "- NI-VISA is installed",
                    "Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                throw; // Re-throw to prevent application from continuing with null session
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {

        }

        private void btnRun_Checked(object sender, RoutedEventArgs e)
        {
            btnRun.Content = "Stop";
            ReadTimer.Start();
        }

        private void btnRun_Unchecked(object sender, RoutedEventArgs e)
        {
            btnRun.Content = "Run";
            ReadTimer.Stop();
        }

        private void ExecutedSetModeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            RadioButton btnClicked = e.Source as RadioButton;

            SetMode(btnClicked.CommandParameter.ToString());
        }

        private void SetMode(string mode)
        {
            switch (mode)
            {
                case "DCV":
                    CurrentMode = Mode.DCV;
                    CurrentCommand = "MEAS:VOLT:DC?";
                    break;
                case "ACV":
                    CurrentMode = Mode.ACV;
                    CurrentCommand = "MEAS:VOLT:AC?";
                    break;
                case "DCI":
                    CurrentMode = Mode.DCI;
                    CurrentCommand = "MEAS:CURR:DC?";
                    break;
                case "ACI":
                    CurrentMode = Mode.ACI;
                    CurrentCommand = "MEAS:CURR:DC?";
                    break;
                case "OHM":
                    CurrentMode = Mode.OHM;
                    CurrentCommand = "MEAS:RES?";
                    break;
            }

            SendCommand(CurrentCommand);
        }

        private void SendCommand(string Command)
        {
            try
            {
                if (TcpipSession?.FormattedIO == null)
                    throw new InvalidOperationException("VISA session not initialized");
                    
                TcpipSession.FormattedIO.WriteLine(Command);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to send command: {Command}\n\n" +
                    $"Error: {ex.Message}",
                    "Communication Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private string ReadCommand(string Command)
        {
            try
            {
                if (TcpipSession?.FormattedIO == null)
                    throw new InvalidOperationException("VISA session not initialized");
                    
                TcpipSession.FormattedIO.WriteLine(Command);
                return TcpipSession.FormattedIO.ReadString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to read command: {Command}\n\n" +
                    $"Error: {ex.Message}",
                    "Communication Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return string.Empty;
            }
        }

        private void CanExecuteSetModeCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            Control target = e.Source as Control;

            if (target != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                string Symbol = "";
                
                switch (CurrentMode)
                {
                    case Mode.DCV:
                    case Mode.ACV:
                        Symbol = "V";
                        break;
                    case Mode.DCI:
                    case Mode.ACI:
                        Symbol = "A";
                        break;
                    case Mode.OHM:
                        Symbol = "Ω";
                        break;
                }
                
                string response = ReadCommand(CurrentCommand);
                if (string.IsNullOrWhiteSpace(response))
                {
                    txtReading.Text = "Error: No Response";
                    ReadTimer.Stop();
                    return;
                }
                
                if (!double.TryParse(response, out double value))
                {
                    txtReading.Text = $"Error: Invalid Data ({response})";
                    ReadTimer.Stop();
                    return;
                }
                
                txtReading.Text = ToEngineeringFormat.Convert(value, 6, Symbol);
            }
            catch (Exception ex)
            {
                txtReading.Text = "Error: Communication Failed";
                ReadTimer.Stop();
                MessageBox.Show(
                    $"Communication error during measurement.\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    "Measurement stopped. Check device connection and click Run to restart.",
                    "Communication Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
