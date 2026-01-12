using Ivi.Visa.FormattedIO;
using NationalInstruments.Visa;
using Ivi.Visa;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DM3058
{
    enum Mode {DCV, ACV, DCI, ACI, OHM };

    /// <summary>
    /// Constants for DMM measurement modes used in UI bindings and command routing
    /// </summary>
    public static class ModeConstants
    {
        public const string DCV = "DCV";
        public const string ACV = "ACV";
        public const string DCI = "DCI";
        public const string ACI = "ACI";
        public const string OHM = "OHM";
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static RoutedCommand SetModeCommand { get; } = new RoutedCommand();

        private string _dmmAddress = @"TCPIP0::192.168.1.213::inst0::INSTR";
        private readonly ResourceManager _resMgr = new ResourceManager();
        private DispatcherTimer _readTimer;
        private Mode _currentMode;
        private string _currentCommand;
        private TcpipSession _tcpipSession;
        private bool _isReading = false;

        public MainWindow()
        {
            InitializeComponent();

            CommandBinding SetModeCommandBinding = new CommandBinding(SetModeCommand, ExecutedSetModeCommand, CanExecuteSetModeCommand);
            this.CommandBindings.Add(SetModeCommandBinding);

            InitializeDMM();
            SetMode(ModeConstants.DCV);

            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _readTimer = new DispatcherTimer();
            _readTimer.Interval = TimeSpan.FromSeconds(1);
            _readTimer.Tick += Timer_Tick;
        }

        private void InitializeDMM()
        {
            try
            {
                _tcpipSession = (TcpipSession)_resMgr.Open(_dmmAddress);
                _tcpipSession.TerminationCharacterEnabled = true;
                _tcpipSession.TimeoutMilliseconds = 20000;
                _tcpipSession.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to connect to DMM at {_dmmAddress}\n\n" +
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

        private void btnRun_Checked(object sender, RoutedEventArgs e)
        {
            btnRun.Content = "Stop";
            _readTimer.Start();
        }

        private void btnRun_Unchecked(object sender, RoutedEventArgs e)
        {
            btnRun.Content = "Run";
            _readTimer.Stop();
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
                case ModeConstants.DCV:
                    _currentMode = Mode.DCV;
                    _currentCommand = "MEAS:VOLT:DC?";
                    break;
                case ModeConstants.ACV:
                    _currentMode = Mode.ACV;
                    _currentCommand = "MEAS:VOLT:AC?";
                    break;
                case ModeConstants.DCI:
                    _currentMode = Mode.DCI;
                    _currentCommand = "MEAS:CURR:DC?";
                    break;
                case ModeConstants.ACI:
                    _currentMode = Mode.ACI;
                    _currentCommand = "MEAS:CURR:AC?";
                    break;
                case ModeConstants.OHM:
                    _currentMode = Mode.OHM;
                    _currentCommand = "MEAS:RES?";
                    break;
            }

            SendCommand(_currentCommand);
        }

        private void SendCommand(string command)
        {
            try
            {
                if (_tcpipSession?.FormattedIO == null)
                    throw new InvalidOperationException("VISA session not initialized");
                    
                _tcpipSession.FormattedIO.WriteLine(command);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to send command: {command}\n\n" +
                    $"Error: {ex.Message}",
                    "Communication Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private string ReadCommand(string command)
        {
            try
            {
                if (_tcpipSession?.FormattedIO == null)
                    throw new InvalidOperationException("VISA session not initialized");
                    
                _tcpipSession.FormattedIO.WriteLine(command);
                return _tcpipSession.FormattedIO.ReadString();
            }
            catch (Exception)
            {
                // Return empty string and let caller handle the error
                // This prevents modal dialog spam during timer operations
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
            // Prevent overlapping timer ticks when read operation takes longer than timer interval
            if (_isReading)
                return;
            
            _isReading = true;
            try
            {
                string Symbol = "";
                
                switch (_currentMode)
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
                
                string response = ReadCommand(_currentCommand);
                if (string.IsNullOrWhiteSpace(response))
                {
                    txtReading.Text = "Error: No Response";
                    btnRun.IsChecked = false;
                    return;
                }
                
                if (!double.TryParse(response, out double value))
                {
                    txtReading.Text = $"Error: Invalid Data ({response})";
                    btnRun.IsChecked = false;
                    return;
                }
                
                txtReading.Text = ToEngineeringFormat.Convert(value, 6, Symbol);
            }
            catch (Exception ex)
            {
                txtReading.Text = "Error: Communication Failed";
                btnRun.IsChecked = false;
                MessageBox.Show(
                    $"Communication error during measurement.\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    "Measurement stopped. Check device connection and click Run to restart.",
                    "Communication Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isReading = false;
            }
        }

        /// <summary>
        /// Handles the Window.Closing event. Stops the timer and disposes of VISA resources.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _readTimer?.Stop();
            
            try
            {
                _tcpipSession?.Dispose();
                _resMgr?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing VISA resources: {ex.Message}");
            }
        }
    }
}
