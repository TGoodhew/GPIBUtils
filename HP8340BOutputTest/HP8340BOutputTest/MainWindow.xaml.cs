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

namespace HP8340BOutputTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HP8902A hP8902 = new HP8902A();

        public static RoutedCommand settingsCmd = new RoutedCommand();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SettingsCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RadioButton btnClicked = e.Source as RadioButton;

        }

        private void SettingsCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Source is Control)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            hP8902.Connect();
        }

    }
}
