using System.Text.RegularExpressions;
using System.Windows;

namespace DM3058
{
    /// <summary>
    /// Dialog window for configuring the DM3058 multimeter TCPIP address.
    /// </summary>
    public partial class ConfigDialog : Window
    {
        // Basic format validation - allows 1-3 digits per octet, detailed range check follows
        private static readonly Regex IpRegex = new Regex(@"^(\d{1,3}\.){3}\d{1,3}$");

        /// <summary>
        /// Gets the configured TCPIP address in VISA format.
        /// </summary>
        public string TCPIPAddress { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigDialog"/> class.
        /// </summary>
        /// <param name="currentAddress">The current IP address (without VISA prefix/suffix).</param>
        public ConfigDialog(string currentAddress)
        {
            InitializeComponent();
            AddressTextBox.Text = currentAddress;
            AddressTextBox.Focus();
            AddressTextBox.SelectAll();
        }

        /// <summary>
        /// Validates that the input is a valid IP address format.
        /// </summary>
        /// <param name="address">The IP address to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private bool ValidateIPAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                StatusLabel.Text = "IP address cannot be empty.";
                return false;
            }

            // Simple IP address validation (xxx.xxx.xxx.xxx)
            if (!IpRegex.IsMatch(address))
            {
                StatusLabel.Text = "Invalid IP address format. Expected format: xxx.xxx.xxx.xxx";
                return false;
            }

            // Validate each octet is 0-255
            var parts = address.Split('.');
            foreach (var part in parts)
            {
                if (int.TryParse(part, out int octet))
                {
                    if (octet < 0 || octet > 255)
                    {
                        StatusLabel.Text = "IP address octets must be between 0 and 255.";
                        return false;
                    }
                }
                else
                {
                    StatusLabel.Text = "Invalid IP address format.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Handles the OK button click. Validates the address and closes the dialog if valid.
        /// </summary>
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            string address = AddressTextBox.Text.Trim();

            if (ValidateIPAddress(address))
            {
                // Return full VISA format address
                TCPIPAddress = $"TCPIP0::{address}::inst0::INSTR";
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// Handles the Cancel button click. Closes the dialog without saving changes.
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
