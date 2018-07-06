using Ivi.Visa.Interop;
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

namespace HP438A
{
    enum Mode { CHA, CHB, CAL, ADJ, SWR };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static RoutedCommand SetModeCommand = new RoutedCommand();

        private string PWRMeterAddress = @"";
        private ResourceManager ResMgr = new ResourceManager();
        private FormattedIO488 PWRMeter = new FormattedIO488();
        private DispatcherTimer ReadTimer;
        private Mode CurrentMode;
        private string CurrentCommand;

        public MainWindow()
        {
            InitializeComponent();

            CommandBinding SetModeCommandBinding = new CommandBinding(SetModeCommand, ExecutedSetModeCommand, CanExecuteSetModeCommand);
            this.CommandBindings.Add(SetModeCommandBinding);

            Initialize438();
            SetMode("CHA");

            InitializeTimer();
        }

        private void Initialize438()
        {
            PWRMeter.IO = (IMessage)ResMgr.Open(PWRMeterAddress, AccessMode.NO_LOCK, 2000, null);
            PWRMeter.IO.TerminationCharacterEnabled = true;
            PWRMeter.IO.Timeout = 20000;
            PWRMeter.IO.Clear();

            // PReset, ZEro, CaL100, ENter, LoG, TRigger3
            SendCommand("PRZECL100ENLGTR3");
        }

        private void InitializeTimer()
        {
            ReadTimer = new DispatcherTimer();
            ReadTimer.Interval = TimeSpan.FromSeconds(1);
            ReadTimer.Tick += Timer_Tick;
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
                case "CHA":
                    CurrentMode = Mode.CHA;
                    CurrentCommand = "AP";
                    break;
                case "CHB":
                    CurrentMode = Mode.CHB;
                    CurrentCommand = "BP";
                    break;
                case "CAL":
                    CurrentMode = Mode.CAL;
                    CurrentCommand = "";
                    break;
                case "ADJ":
                    CurrentMode = Mode.ADJ;
                    CurrentCommand = "";
                    break;
                case "SWR":
                    CurrentMode = Mode.SWR;
                    CurrentCommand = "";
                    break;
            }

            SendCommand(CurrentCommand);
        }

        private void SendCommand(string Command)
        {
            PWRMeter.WriteString(Command, true);
        }

        private string ReadCommand(string Command)
        {
            PWRMeter.WriteString(Command, true);
            return PWRMeter.ReadString();
        }

        private void CanExecuteSetModeCommand(object sender, CanExecuteRoutedEventArgs e)
        {

            if (e.Source is Control target)
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
                case Mode.CHA:
                case Mode.CHB:
                    Symbol = "dBm";
                    break;
                case Mode.CAL:
                case Mode.ADJ:
                    Symbol = "";
                    break;
                case Mode.SWR:
                    Symbol = "SWR";
                    break;
            }
            txtReading.Text = ToEngineeringFormat.Convert(Convert.ToDouble(ReadCommand(CurrentCommand)), 6, Symbol);
        }
    }
}
