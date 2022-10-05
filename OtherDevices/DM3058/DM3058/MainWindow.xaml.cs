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
            TcpipSession = (TcpipSession)ResMgr.Open(DMMAddress);
            TcpipSession.TerminationCharacterEnabled = true;
            TcpipSession.TimeoutMilliseconds = 20000;
            TcpipSession.Clear();
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
            TcpipSession.FormattedIO.WriteLine(Command);
        }

        private string ReadCommand(string Command)
        {
            TcpipSession.FormattedIO.WriteLine(Command);
            return TcpipSession.FormattedIO.ReadString();
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
                    Symbol = "Ω";
                    break;
            }
            txtReading.Text = ToEngineeringFormat.Convert(Convert.ToDouble(ReadCommand(CurrentCommand)),6,Symbol);
        }
    }
}
