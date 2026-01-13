using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace DM3058
{
    /// <summary>
    /// Interaction logic for LogConfigDialog.xaml
    /// </summary>
    public partial class LogConfigDialog : Window
    {
        public string LogFilePath { get; private set; }
        public string LogFormat { get; private set; }

        public LogConfigDialog(string currentPath, string currentFormat)
        {
            InitializeComponent();

            // Set current values
            txtLogPath.Text = currentPath;
            
            if (currentFormat?.ToUpper() == "XML")
            {
                rbXML.IsChecked = true;
            }
            else
            {
                rbCSV.IsChecked = true;
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "Select Log File Location",
                Filter = "CSV Files (*.csv)|*.csv|XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                FilterIndex = rbCSV.IsChecked == true ? 1 : 2,
                AddExtension = true,
                CheckPathExists = true,
                OverwritePrompt = false // Don't warn about overwrite since we append
            };

            // If there's already a path, use it as the initial directory
            if (!string.IsNullOrWhiteSpace(txtLogPath.Text))
            {
                try
                {
                    string directory = Path.GetDirectoryName(txtLogPath.Text);
                    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    {
                        dialog.InitialDirectory = directory;
                        dialog.FileName = Path.GetFileName(txtLogPath.Text);
                    }
                }
                catch
                {
                    // If path parsing fails, just use default directory
                }
            }

            if (dialog.ShowDialog() == true)
            {
                txtLogPath.Text = dialog.FileName;
                
                // Update format based on file extension
                string extension = Path.GetExtension(dialog.FileName)?.ToLower();
                if (extension == ".xml")
                {
                    rbXML.IsChecked = true;
                }
                else if (extension == ".csv")
                {
                    rbCSV.IsChecked = true;
                }
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(txtLogPath.Text))
            {
                MessageBox.Show(
                    "Please specify a log file path.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtLogPath.Focus();
                return;
            }

            // Validate that the directory exists or can be created
            try
            {
                string directory = Path.GetDirectoryName(txtLogPath.Text);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    var result = MessageBox.Show(
                        $"The directory does not exist:\n{directory}\n\nWould you like to create it?",
                        "Directory Not Found",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        Directory.CreateDirectory(directory);
                    }
                    else
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Invalid file path:\n\n{ex.Message}",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtLogPath.Focus();
                return;
            }

            LogFilePath = txtLogPath.Text;
            LogFormat = rbXML.IsChecked == true ? "XML" : "CSV";
            
            DialogResult = true;
            Close();
        }
    }
}
