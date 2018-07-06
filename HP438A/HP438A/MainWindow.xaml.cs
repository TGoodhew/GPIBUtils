﻿using Ivi.Visa.Interop;
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
    enum Mode { CHA, CHB, ZER, CAL, ADJ, SWR };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IEventHandler
    {
        public static RoutedCommand SetModeCommand = new RoutedCommand();

        private string PWRMeterAddress = @"GPIB1::13::INSTR";
        private ResourceManager ResMgr = new ResourceManager();
        private FormattedIO488 PWRMeter = new FormattedIO488();
        private DispatcherTimer ReadTimer;
        private Mode CurrentMode;
        private string CurrentCommand;
        private Mode CurrentChannel;
        private IEventManager Srq;

        public MainWindow()
        {
            InitializeComponent();

            CommandBinding SetModeCommandBinding = new CommandBinding(SetModeCommand, ExecutedSetModeCommand, CanExecuteSetModeCommand);
            this.CommandBindings.Add(SetModeCommandBinding);

            Initialize438();
            SetMode("CHA");
            CurrentChannel = Mode.CHA;

            InitializeTimer();
        }

        private void Initialize438()
        {
            PWRMeter.IO = (IMessage)ResMgr.Open(PWRMeterAddress, AccessMode.NO_LOCK, 2000, null);
            PWRMeter.IO.TerminationCharacterEnabled = true;
            PWRMeter.IO.Timeout = 20000;
            PWRMeter.IO.Clear();

            // PReset, CaL100, ENter, LoG, TRigger3
            SendCommand("PRCSLGTR3");
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

        private void SetMode(Mode targetMode)
        {
            switch (targetMode)
            {
                case Mode.CHA:
                    SetMode("CHA");
                    break;
                case Mode.CHB:
                    SetMode("CHB");
                    break;
                case Mode.ZER:
                    SetMode("ZER");
                    break;
                case Mode.CAL:
                    SetMode("CAL");
                    break;
                case Mode.ADJ:
                    SetMode("ADJ");
                    break;
                case Mode.SWR:
                    SetMode("SWR");
                    break;
                default:
                    break;
            }
        }

        private void SetMode(string mode)
        {
            switch (mode)
            {
                case "CHA":
                    CurrentMode = Mode.CHA;
                    CurrentCommand = "AP";
                    CurrentChannel = Mode.CHA;
                    break;
                case "CHB":
                    CurrentMode = Mode.CHB;
                    CurrentCommand = "BP";
                    CurrentChannel = Mode.CHB;
                    break;
                case "ZER":
                    CurrentMode = Mode.ZER;
                    //Set the SRQ Mask
                    // The Cal/Zero mask is bit 2 (bit 1 if zero referencing) 
                    // so that resolves to a 0x02 byte which can be put in a 
                    // string using the unicode escape
                    SendCommand("@1\u0002"); 
                                             
                    // Setup the event handler for SRQ (primarily for CAL & ZERO)
                    Srq = (IEventManager)PWRMeter.IO;
                    Srq.InstallHandler(EventType.EVENT_SERVICE_REQ, this);
                    Srq.EnableEvent(EventType.EVENT_SERVICE_REQ, EventMechanism.EVENT_HNDLR);

                    // Zero the meter
                    CurrentCommand = "ZE";
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
            //TODO: Fix condition when read returns an error string
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
                    //TODO: Add support for Log/Lin
                    //TODO: Watch for Log/Lin setting to determine whether to use engineering notation
                    //txtReading.Text = ToEngineeringFormat.Convert(Convert.ToDouble(ReadCommand(CurrentCommand)), 6, Symbol);
                    var reading = Convert.ToDouble(ReadCommand(CurrentCommand));
                    Symbol = " dBm";
                    txtReading.Text = reading.ToString("F") + Symbol;
                    break;
                case Mode.ZER:
                    txtReading.Text = "Zeroing";
                    break;
                case Mode.CAL:
                case Mode.ADJ:
                    Symbol = "";
                    break;
                case Mode.SWR:
                    Symbol = " SWR";
                    break;
            }
        }

        void IEventHandler.HandleEvent(IEventManager vi, IEvent @event, int userHandle)
        {
            switch (CurrentMode)
            {
                case Mode.ZER:
                case Mode.CAL:
                    SetMode(CurrentChannel);
                    var radioButtons = ModeButtons.Children.OfType<RadioButton>();
                    foreach (var radioButton in radioButtons)
                    {
                        if (radioButton.IsChecked ?? false)
                        {
                            var name = radioButton.Name;
                            var index = radioButtons.ToList().IndexOf(radioButton);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
