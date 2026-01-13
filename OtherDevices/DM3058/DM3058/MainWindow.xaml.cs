using Ivi.Visa.FormattedIO;
using NationalInstruments.Visa;
using Ivi.Visa;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using System.Xml.Linq;
using System.Globalization;

namespace DM3058
{
    /// <summary>
    /// Measurement modes for the DM3058 Digital Multimeter.
    /// </summary>
    enum Mode 
    {
        /// <summary>
        /// DC Voltage measurement mode.
        /// </summary>
        DCV, 
        /// <summary>
        /// AC Voltage measurement mode.
        /// </summary>
        ACV, 
        /// <summary>
        /// DC Current measurement mode.
        /// </summary>
        DCI, 
        /// <summary>
        /// AC Current measurement mode.
        /// </summary>
        ACI, 
        /// <summary>
        /// Resistance (Ohms) measurement mode.
        /// </summary>
        OHM 
    };

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

        private const string DefaultIPAddress = "192.168.1.213";
        private const double IntervalComparisonTolerance = 0.01;
        private static readonly Regex VisaAddressRegex = new Regex(@"TCPIP\d+::([^:]+)::.*");
        
        private readonly string _dmmAddress;
        private readonly string _deviceIPAddress;
        private readonly ResourceManager _resMgr = new ResourceManager();
        private DispatcherTimer _readTimer;
        private Mode _currentMode;
        private string _currentCommand;
        private TcpipSession _tcpipSession;
        private bool _isReading = false;
        private double _timerIntervalSeconds = 1.0;
        private bool _isInitialized = false;
        private bool _isUpdatingInterval = false;
        private DateTime _lastSuccessfulUpdate = DateTime.MinValue;
        private bool _isConnected = false;
        
        // Logging-related fields
        private bool _isLogging = false;
        private StreamWriter _logWriter;
        private XDocument _xmlLog;
        private string _currentLogPath;
        private string _currentLogFormat;
        
        // Cache brushes to avoid repeated allocation and freeze for WPF optimization
        private static readonly SolidColorBrush GrayBrush = CreateFrozenBrush(Colors.Gray);
        private static readonly SolidColorBrush YellowBrush = CreateFrozenBrush(Colors.Yellow);
        private static readonly SolidColorBrush GreenBrush = CreateFrozenBrush(Colors.Green);
        private static readonly SolidColorBrush RedBrush = CreateFrozenBrush(Colors.Red);
        private static readonly SolidColorBrush OrangeBrush = CreateFrozenBrush(Colors.Orange);

        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public MainWindow()
        {
            InitializeComponent();

            CommandBinding SetModeCommandBinding = new CommandBinding(SetModeCommand, ExecutedSetModeCommand, CanExecuteSetModeCommand);
            this.CommandBindings.Add(SetModeCommandBinding);

            // Load IP address from settings, default to original hardcoded value if not set
            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.TCPIPAddress))
            {
                Properties.Settings.Default.TCPIPAddress = DefaultIPAddress;
                Properties.Settings.Default.Save();
            }

            _dmmAddress = BuildVISAAddress(Properties.Settings.Default.TCPIPAddress);
            _deviceIPAddress = ExtractIPFromVISA(_dmmAddress);

            InitializeDMM();
            SetMode(ModeConstants.DCV);

            InitializeTimer();
            
            // Load logging settings
            _currentLogPath = Properties.Settings.Default.LogFilePath;
            _currentLogFormat = Properties.Settings.Default.LogFormat;
            
            // Initialize logging status display
            UpdateLoggingStatus();
            
            _isInitialized = true;
        }

        private void InitializeTimer()
        {
            _readTimer = new DispatcherTimer();
            _readTimer.Interval = TimeSpan.FromSeconds(_timerIntervalSeconds);
            _readTimer.Tick += Timer_Tick;
        }

        private void InitializeDMM()
        {
            UpdateStatus("Connecting...", YellowBrush);
            try
            {
                _tcpipSession = (TcpipSession)_resMgr.Open(_dmmAddress);
                _tcpipSession.TerminationCharacterEnabled = true;
                _tcpipSession.TimeoutMilliseconds = 20000;
                _tcpipSession.Clear();
                
                // Verify device responds before marking as connected
                _tcpipSession.FormattedIO.WriteLine("*IDN?");
                string response = _tcpipSession.FormattedIO.ReadString();
                
                if (string.IsNullOrWhiteSpace(response))
                {
                    throw new InvalidOperationException("Device did not respond to identification query");
                }
                
                // Clear the input buffer to prevent *IDN? response from being read by first measurement
                try
                {
                    _tcpipSession.FormattedIO.DiscardBuffers();
                }
                catch
                {
                    // Ignore discard errors
                }
                
                _isConnected = true;
                UpdateStatus($"Connected to {_deviceIPAddress}", GreenBrush);
            }
            catch (Exception ex)
            {
                _isConnected = false;
                UpdateStatus("Connection failed", RedBrush);
                
                // Dispose of session and ResourceManager if opened but device didn't respond properly
                try
                {
                    _tcpipSession?.Dispose();
                    _tcpipSession = null;
                }
                catch (Exception)
                {
                    // Ignore disposal errors
                }
                
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
            
            // Don't send command here - it will be sent by ReadCommand when timer ticks
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
                string ModeStr = "";
                
                switch (_currentMode)
                {
                    case Mode.DCV:
                        Symbol = "V";
                        ModeStr = "DCV";
                        break;
                    case Mode.ACV:
                        Symbol = "V";
                        ModeStr = "ACV";
                        break;
                    case Mode.DCI:
                        Symbol = "A";
                        ModeStr = "DCI";
                        break;
                    case Mode.ACI:
                        Symbol = "A";
                        ModeStr = "ACI";
                        break;
                    case Mode.OHM:
                        Symbol = "Ω";
                        ModeStr = "OHM";
                        break;
                }
                
                string response = ReadCommand(_currentCommand);
                if (string.IsNullOrWhiteSpace(response))
                {
                    txtReading.Text = "Error: No Response";
                    btnRun.IsChecked = false;
                    _isConnected = false;
                    UpdateStatus("Error: Device not responding", RedBrush);
                    return;
                }
                
                if (!double.TryParse(response, out double value))
                {
                    txtReading.Text = $"Error: Invalid Data ({response})";
                    btnRun.IsChecked = false;
                    _isConnected = false;
                    UpdateStatus("Error: Invalid data from device", RedBrush);
                    return;
                }
                
                txtReading.Text = ToEngineeringFormat.Convert(value, 6, Symbol);
                
                // Log the reading if logging is enabled
                LogReading(value, Symbol, ModeStr);
                
                // Update status on successful reading
                _lastSuccessfulUpdate = DateTime.Now;
                if (!_isConnected)
                {
                    _isConnected = true;
                    UpdateStatus($"Connected to {_deviceIPAddress}", GreenBrush);
                }
                UpdateLastUpdateTime();
            }
            catch (Exception ex)
            {
                txtReading.Text = "Error: Communication Failed";
                btnRun.IsChecked = false;
                _isConnected = false;
                UpdateStatus("Error: Communication failed", RedBrush);
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
        /// Updates the status indicator and status text in the status bar.
        /// </summary>
        /// <param name="message">The status message to display.</param>
        /// <param name="brush">The brush for the status indicator.</param>
        private void UpdateStatus(string message, SolidColorBrush brush)
        {
            if (statusIndicator != null)
                statusIndicator.Fill = brush;
            if (statusText != null)
                statusText.Text = message;
        }

        /// <summary>
        /// Updates the last update timestamp display in the status bar.
        /// </summary>
        private void UpdateLastUpdateTime()
        {
            if (lastUpdateText != null)
            {
                lastUpdateText.Text = _lastSuccessfulUpdate != DateTime.MinValue 
                    ? $"Last update: {_lastSuccessfulUpdate:HH:mm:ss}" 
                    : "Last update: --";
            }
        }

        /// <summary>
        /// Handles the Test Connection button click event.
        /// Tests the connection to the DMM and updates the status accordingly.
        /// Pauses measurements during test and resumes them afterwards if they were running.
        /// </summary>
        private void btnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            // Pause measurements if running to avoid concurrent access
            bool wasRunning = _readTimer?.IsEnabled == true;
            if (wasRunning)
            {
                _readTimer.Stop();
            }
            
            // Wait for any in-progress reading to complete
            while (_isReading)
            {
                System.Threading.Thread.Sleep(10);
                System.Windows.Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            }
            
            UpdateStatus("Testing connection...", YellowBrush);
            
            try
            {
                if (_tcpipSession == null || _tcpipSession.FormattedIO == null)
                {
                    throw new InvalidOperationException("VISA session not initialized. Please restart the application.");
                }
                
                // Send *IDN? query to test connection
                _tcpipSession.FormattedIO.WriteLine("*IDN?");
                string response = _tcpipSession.FormattedIO.ReadString();
                
                if (!string.IsNullOrWhiteSpace(response))
                {
                    _isConnected = true;
                    UpdateStatus($"Connected to {_deviceIPAddress}", GreenBrush);
                    
                    // Clear the input buffer to prevent *IDN? response from being read by next measurement
                    try
                    {
                        _tcpipSession.FormattedIO.DiscardBuffers();
                    }
                    catch
                    {
                        // Ignore discard errors
                    }
                    
                    MessageBox.Show(
                        $"Connection test successful!\n\n" +
                        $"Device: {response.Trim()}\n" +
                        $"Address: {_deviceIPAddress}",
                        "Connection Test",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    throw new InvalidOperationException("Device responded with empty response");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                UpdateStatus("Connection test failed", RedBrush);
                
                MessageBox.Show(
                    $"Connection test failed.\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    "Please check:\n" +
                    "- Device is powered on and connected to network\n" +
                    "- IP address is correct (File → Settings)\n" +
                    "- NI-VISA is installed",
                    "Connection Test Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Resume measurements if they were running before the test
                if (wasRunning)
                {
                    _readTimer.Start();
                }
            }
        }

        /// <summary>
        /// Handles the Window.Closing event. Stops the timer and disposes of VISA resources.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _readTimer?.Stop();
            
            // Stop logging if active
            if (_isLogging)
            {
                StopLogging();
            }
            
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

        /// <summary>
        /// Handles the Settings menu item click. Opens the configuration dialog.
        /// </summary>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConfigDialog(Properties.Settings.Default.TCPIPAddress);
            if (dialog.ShowDialog() == true)
            {
                SaveIPAddressFromDialog(dialog.TCPIPAddress);
                
                MessageBox.Show(
                    "Settings saved successfully.\n\nPlease restart the application for changes to take effect.",
                    "Settings Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Saves the IP address from a dialog to application settings.
        /// </summary>
        /// <param name="visaAddress">The VISA address string from the dialog.</param>
        private void SaveIPAddressFromDialog(string visaAddress)
        {
            string ipAddress = ExtractIPFromVISA(visaAddress);
            Properties.Settings.Default.TCPIPAddress = ipAddress;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Builds a VISA TCPIP resource string from an IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address (e.g., "192.168.1.213").</param>
        /// <returns>The VISA address string (e.g., "TCPIP0::192.168.1.213::inst0::INSTR").</returns>
        private string BuildVISAAddress(string ipAddress)
        {
            return $"TCPIP0::{ipAddress}::inst0::INSTR";
        }

        /// <summary>
        /// Extracts the IP address from a VISA TCPIP resource string.
        /// </summary>
        /// <param name="visaAddress">The VISA address string (e.g., "TCPIP0::192.168.1.213::inst0::INSTR").</param>
        /// <returns>The IP address (e.g., "192.168.1.213").</returns>
        private string ExtractIPFromVISA(string visaAddress)
        {
            // Expected format: TCPIP0::xxx.xxx.xxx.xxx::inst0::INSTR
            var match = VisaAddressRegex.Match(visaAddress);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            // If extraction fails, return default address
            return DefaultIPAddress;
        }

        /// <summary>
        /// Handles the Exit menu item click. Closes the application.
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles the ComboBox selection changed event for timer interval.
        /// Updates the timer interval based on the selected value.
        /// </summary>
        private void cmbInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Avoid processing during initialization or recursive updates
            if (!_isInitialized || _isUpdatingInterval || cmbInterval == null)
                return;
                
            if (cmbInterval.SelectedItem is ComboBoxItem selectedItem && 
                double.TryParse(selectedItem.Tag?.ToString(), out double interval))
            {
                _isUpdatingInterval = true;
                try
                {
                    UpdateTimerInterval(interval);
                    UpdateMenuItemChecks(interval);
                }
                finally
                {
                    _isUpdatingInterval = false;
                }
            }
        }

        /// <summary>
        /// Handles the menu item click event for timer interval selection.
        /// Updates the timer interval and synchronizes with the ComboBox.
        /// </summary>
        private void IntervalMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                double.TryParse(menuItem.Tag?.ToString(), out double interval))
            {
                _isUpdatingInterval = true;
                try
                {
                    UpdateTimerInterval(interval);
                    UpdateMenuItemChecks(interval);
                    UpdateComboBoxSelection(interval);
                }
                finally
                {
                    _isUpdatingInterval = false;
                }
            }
        }

        /// <summary>
        /// Updates the timer interval to the specified value.
        /// If the timer is running, it restarts with the new interval.
        /// </summary>
        private void UpdateTimerInterval(double seconds)
        {
            _timerIntervalSeconds = seconds;
            
            if (_readTimer != null)
            {
                bool wasRunning = _readTimer.IsEnabled;
                _readTimer.Stop();
                _readTimer.Interval = TimeSpan.FromSeconds(_timerIntervalSeconds);
                
                if (wasRunning)
                {
                    _readTimer.Start();
                }
            }
        }

        /// <summary>
        /// Updates the checked state of menu items based on the selected interval.
        /// </summary>
        private void UpdateMenuItemChecks(double interval)
        {
            if (menuUpdateInterval == null)
                return;
                
            foreach (var subItem in menuUpdateInterval.Items)
            {
                if (subItem is MenuItem menuItem && 
                    double.TryParse(menuItem.Tag?.ToString(), out double itemInterval))
                {
                    menuItem.IsChecked = Math.Abs(itemInterval - interval) < IntervalComparisonTolerance;
                }
            }
        }

        /// <summary>
        /// Updates the ComboBox selection based on the specified interval.
        /// </summary>
        private void UpdateComboBoxSelection(double interval)
        {
            if (cmbInterval == null)
                return;
                
            for (int i = 0; i < cmbInterval.Items.Count; i++)
            {
                if (cmbInterval.Items[i] is ComboBoxItem item && 
                    double.TryParse(item.Tag?.ToString(), out double itemInterval) &&
                    Math.Abs(itemInterval - interval) < IntervalComparisonTolerance)
                {
                    cmbInterval.SelectedIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Handles the Logging Configuration menu item click. Opens the logging configuration dialog.
        /// </summary>
        private void LogConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new LogConfigDialog(_currentLogPath, _currentLogFormat);
            if (dialog.ShowDialog() == true)
            {
                // Stop logging if currently active
                bool wasLogging = _isLogging;
                if (_isLogging)
                {
                    StopLogging();
                    btnLogging.IsChecked = false;
                }

                _currentLogPath = dialog.LogFilePath;
                _currentLogFormat = dialog.LogFormat;
                
                // Save settings
                Properties.Settings.Default.LogFilePath = _currentLogPath;
                Properties.Settings.Default.LogFormat = _currentLogFormat;
                Properties.Settings.Default.Save();
                
                UpdateLoggingStatus();
                
                MessageBox.Show(
                    $"Logging configuration saved.\n\n" +
                    $"File: {_currentLogPath}\n" +
                    $"Format: {_currentLogFormat}",
                    "Configuration Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                // If was logging, inform user they need to restart it
                if (wasLogging)
                {
                    MessageBox.Show(
                        "Logging was stopped to apply the new configuration.\n\n" +
                        "Click the 'Log' button to start logging with the new settings.",
                        "Logging Stopped",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// Handles the logging toggle button checked event. Starts logging.
        /// </summary>
        private void btnLogging_Checked(object sender, RoutedEventArgs e)
        {
            StartLogging();
        }

        /// <summary>
        /// Handles the logging toggle button unchecked event. Stops logging.
        /// </summary>
        private void btnLogging_Unchecked(object sender, RoutedEventArgs e)
        {
            StopLogging();
        }

        /// <summary>
        /// Starts logging measurements to file.
        /// </summary>
        private void StartLogging()
        {
            // Validate configuration
            if (string.IsNullOrWhiteSpace(_currentLogPath))
            {
                MessageBox.Show(
                    "Logging is not configured.\n\n" +
                    "Please configure logging from File → Logging Configuration.",
                    "Logging Not Configured",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                btnLogging.IsChecked = false;
                return;
            }

            try
            {
                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(_currentLogPath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (_currentLogFormat?.ToUpper() == "XML")
                {
                    InitializeXMLLogging();
                }
                else
                {
                    InitializeCSVLogging();
                }

                _isLogging = true;
                UpdateLoggingStatus();
            }
            catch (Exception ex)
            {
                _isLogging = false;
                btnLogging.IsChecked = false;
                UpdateLoggingStatus();
                
                MessageBox.Show(
                    $"Failed to start logging:\n\n{ex.Message}",
                    "Logging Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Stops logging measurements.
        /// </summary>
        private void StopLogging()
        {
            _isLogging = false;

            try
            {
                if (_currentLogFormat?.ToUpper() == "XML" && _xmlLog != null)
                {
                    FinalizeXMLLogging();
                }

                _logWriter?.Flush();
                _logWriter?.Close();
                _logWriter?.Dispose();
                _logWriter = null;
                _xmlLog = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error while closing log file:\n\n{ex.Message}",
                    "Logging Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            UpdateLoggingStatus();
        }

        /// <summary>
        /// Initializes CSV logging by creating or appending to the log file.
        /// </summary>
        private void InitializeCSVLogging()
        {
            bool fileExists = File.Exists(_currentLogPath);
            
            _logWriter = new StreamWriter(_currentLogPath, append: true);
            
            // Write header if file is new
            if (!fileExists || new FileInfo(_currentLogPath).Length == 0)
            {
                _logWriter.WriteLine("Timestamp,Mode,Value,Unit");
            }
            
            _logWriter.WriteLine($"# Logging started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logWriter.Flush();
        }

        /// <summary>
        /// Initializes XML logging by creating or loading the log file.
        /// </summary>
        private void InitializeXMLLogging()
        {
            if (File.Exists(_currentLogPath))
            {
                // Load existing XML file
                try
                {
                    _xmlLog = XDocument.Load(_currentLogPath);
                }
                catch
                {
                    // If file is corrupted, create new
                    _xmlLog = new XDocument(
                        new XDeclaration("1.0", "utf-8", "yes"),
                        new XElement("MeasurementLog")
                    );
                }
            }
            else
            {
                // Create new XML document
                _xmlLog = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement("MeasurementLog")
                );
            }

            // Add session start marker
            _xmlLog.Root.Add(new XElement("Session",
                new XAttribute("StartTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            ));
        }

        /// <summary>
        /// Finalizes XML logging by saving the document to file.
        /// </summary>
        private void FinalizeXMLLogging()
        {
            if (_xmlLog != null)
            {
                // Add session end marker
                var lastSession = _xmlLog.Root.Elements("Session").LastOrDefault();
                if (lastSession != null)
                {
                    lastSession.Add(new XAttribute("EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                }
                
                _xmlLog.Save(_currentLogPath);
            }
        }

        /// <summary>
        /// Logs a measurement reading to the currently open log file.
        /// </summary>
        private void LogReading(double value, string unit, string mode)
        {
            if (!_isLogging)
                return;

            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                if (_currentLogFormat?.ToUpper() == "XML")
                {
                    LogReadingXML(timestamp, value, unit, mode);
                }
                else
                {
                    LogReadingCSV(timestamp, value, unit, mode);
                }
            }
            catch (Exception ex)
            {
                // Don't show error dialog during timer tick to avoid spam
                // Just stop logging and update status
                _isLogging = false;
                btnLogging.IsChecked = false;
                UpdateLoggingStatus();
                
                System.Diagnostics.Debug.WriteLine($"Logging error: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs a reading to CSV format.
        /// </summary>
        private void LogReadingCSV(string timestamp, double value, string unit, string mode)
        {
            if (_logWriter != null)
            {
                _logWriter.WriteLine($"{timestamp},{mode},{value.ToString(CultureInfo.InvariantCulture)},{unit}");
                _logWriter.Flush();
            }
        }

        /// <summary>
        /// Logs a reading to XML format.
        /// </summary>
        private void LogReadingXML(string timestamp, double value, string unit, string mode)
        {
            if (_xmlLog != null)
            {
                var lastSession = _xmlLog.Root.Elements("Session").LastOrDefault();
                if (lastSession != null)
                {
                    lastSession.Add(new XElement("Reading",
                        new XAttribute("Timestamp", timestamp),
                        new XAttribute("Mode", mode),
                        new XAttribute("Value", value.ToString(CultureInfo.InvariantCulture)),
                        new XAttribute("Unit", unit)
                    ));
                }
            }
        }

        /// <summary>
        /// Updates the logging status display in the status bar.
        /// </summary>
        private void UpdateLoggingStatus()
        {
            if (loggingStatusText != null && loggingIndicator != null)
            {
                if (_isLogging)
                {
                    loggingStatusText.Text = $"Logging: {_currentLogFormat}";
                    loggingIndicator.Fill = GreenBrush;
                }
                else if (!string.IsNullOrWhiteSpace(_currentLogPath))
                {
                    loggingStatusText.Text = "Logging: Ready";
                    loggingIndicator.Fill = OrangeBrush;
                }
                else
                {
                    loggingStatusText.Text = "Logging: Not configured";
                    loggingIndicator.Fill = GrayBrush;
                }
            }
        }
    }
}
