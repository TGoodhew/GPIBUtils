﻿using Ivi.Visa;
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
    // TODO: Consider removing
    // enum Mode { CHA, CHB, ZER, CAL, ADJ, SWR };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static RoutedCommand SetModeCommand = new RoutedCommand();

        public MainWindow()
        {
            InitializeComponent();

            PwrSensor sensor = new PwrSensor();

            sensor.CalFactorTable.Add(5000000000, 97.1);
            sensor.CalFactorTable.Add(6000000000, 96.7);

            var result = sensor.GetCalFactorForFrequency(6000000000);
        }

        private void ExecutedSetModeCommand(object sender, ExecutedRoutedEventArgs e)
        {
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

        private void btnRun_Checked(object sender, RoutedEventArgs e)
        {
            btnRun.Content = "Stop";
        }

        private void btnRun_Unchecked(object sender, RoutedEventArgs e)
        {
            btnRun.Content = "Run";
        }
    }
}
