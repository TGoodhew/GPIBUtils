using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Ivi.Visa.FormattedIO;
using NationalInstruments.Visa;
using Ivi.Visa;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace HPDevices.HP8673B
{
    /// <summary>
    /// Service Request (SRQ) mask flags for the HP 8673B Synthesized Signal Generator.
    /// </summary>
    /// <remarks>
    /// These flags control which conditions generate a Service Request on the GPIB bus.
    /// Multiple flags can be combined using bitwise OR operations.
    /// </remarks>
    [Flags]
    public enum SRQMaskFlags : short
    {
        /* Bit 7 - Change in Sweep Parameters
         * Bit 6 - Asserted whenever there is an SRQ
         * Bit 5 - Entry Error
         * Bit 4 - End of Sweep
         * Bit 3 - Source Settled
         * Bit 2 - Change in Extended Status Byte
         * Bit 1 - Front Panel Entry Complete
         * Bit 0 - Front Panel Key Pressed
        */
        /// <summary>
        /// Indicates that a front panel key was pressed.
        /// </summary>
        FrontPanelKeyPressed = 0x01,
        /// <summary>
        /// Indicates that a front panel entry is complete.
        /// </summary>
        FrontPanelEntryComplete = 0x02,
        /// <summary>
        /// Indicates a change in the Extended Status Byte.
        /// </summary>
        ChangeInESB = 0x04,
        /// <summary>
        /// Indicates that the RF source has settled to the commanded frequency.
        /// </summary>
        SourceSettled = 0x08,
        /// <summary>
        /// Indicates that a sweep has completed.
        /// </summary>
        EndOfSweep = 0x10,
        /// <summary>
        /// Indicates that an entry error occurred.
        /// </summary>
        EntryError = 0x20,
        /// <summary>
        /// Asserted whenever a Service Request occurs.
        /// </summary>
        SRQAssert = 0x40,
        /// <summary>
        /// Indicates that sweep parameters have changed.
        /// </summary>
        ChangedSweepParameters = 0x80
    }

    /// <summary>
    /// Represents an HP 8673B Synthesized Signal Generator that provides precise RF signal generation
    /// with frequency and power control.
    /// </summary>
    /// <remarks>
    /// The HP 8673B is a high-performance synthesized signal generator with frequency range from
    /// 2 to 18 GHz. This class provides methods to set CW frequency, power level, and RF output state
    /// via GPIB communication. It uses SRQ-based synchronization for frequency settling.
    /// </remarks>
    public class Device : IDisposable
    {
        /// <summary>
        /// Gets the GPIB address for this device in the format "GPIB0::XX::INSTR".
        /// </summary>
        public string gpibAddress { get; }

        private GpibSession gpibSession;
        private ResourceManager resManager;
        private SemaphoreSlim srqWait = new SemaphoreSlim(0, 1); // use a semaphore to wait for the SRQ events
        private bool disposed = false;

        private string lastCommand;

        /// <summary>
        /// Initializes a new instance of the HP 8673B device and establishes GPIB communication.
        /// </summary>
        /// <param name="GPIBAddress">The GPIB address in the format "GPIB0::XX::INSTR" where XX is the device address.</param>
        /// <remarks>
        /// Upon initialization, the SRQ mask is cleared and the device is preset to a known state using
        /// the IP (Instrument Preset) command. The timeout is set to 20 seconds to accommodate
        /// measurements with 1 Hz resolution.
        /// </remarks>
        public Device(string GPIBAddress)
        {
            resManager = new ResourceManager();
            gpibAddress = GPIBAddress;

            gpibSession = (GpibSession)resManager.Open(gpibAddress);
            gpibSession.TimeoutMilliseconds = 20000; // Set the timeout to be 20s to handle 1HZ resolution
            gpibSession.TerminationCharacterEnabled = true;

            gpibSession.Clear();

            gpibSession.ServiceRequest += SRQHandler;

            // Ensure the the SRQ mask is 0
            SendCommand("RM0");

            // Send an instrument preset
            SendCommand("IP");
        }

        /// <summary>
        /// Sets the continuous wave (CW) output frequency and waits for the source to settle.
        /// </summary>
        /// <param name="frequency">The desired frequency in Hertz (Hz).</param>
        /// <returns>The actual locked frequency in Hz, which may differ slightly from the requested frequency above 6.6 GHz.</returns>
        /// <remarks>
        /// The method sets the frequency using the FR command, waits for the source to settle using SRQ,
        /// and then reads back the actual locked frequency. For frequencies above 6.6 GHz, the exact
        /// frequency may not be achievable due to baseband frequency multiplication, so the actual
        /// locked frequency is returned.
        /// </remarks>
        public double SetCWFrequency(double frequency)
        {
            // Setup the SRQ to wait for source to be settled (RM)
            string command = String.Format("RM{0:d}", SRQMaskFlags.SourceSettled);

            SendCommand("RM8");

            // Set the CW frequency in Hz (FR) 
            SendCommand(String.Format("FR{0}HZ", frequency));

            // Wait for the data to be available
            srqWait.Wait();

            // Clear the SRQ mask
            SendCommand("RM0");

            // Frequencies above 6.6GHz may not be set exactly due to multiplication of the baseband frequency
            // so we should read back the frequency to provide that back for tasks such as LO use
            // OK returns the current frequency that is locked
            SendCommand("OK");

            // Get the value
            string result = "FR3000000000HZ";

            result = gpibSession.FormattedIO.ReadString();

            result = Regex.Match(result, @"\d+").Value;

            return double.Parse(result);
        }

        /// <summary>
        /// Sets the output power level.
        /// </summary>
        /// <param name="power">The desired power level in dBm (decibels relative to 1 milliwatt).</param>
        /// <remarks>
        /// The power level is set using the LE command followed by the power value in dBm.
        /// </remarks>
        public void SetPowerLevel(double power)
        {
            // Set the power level (LE)
            SendCommand(String.Format("LE{0}DM", power));
        }

        /// <summary>
        /// Enables or disables the RF output.
        /// </summary>
        /// <param name="output">True to enable RF output; false to disable it.</param>
        /// <remarks>
        /// This method uses the RF command to control the output state. RF0 disables output, RF1 enables it.
        /// </remarks>
        public void EnableRFOutput(bool output)
        {
            if (!output)
                SendCommand("RF0");
            else
                SendCommand("RF1");
        }

        private void SendCommand(string command)
        {
            lastCommand = command; 
            gpibSession.FormattedIO.WriteLine(command);
        }

        private double ReadSciValue()
        {
            // The frequency is read in scientific notation as Hz
            gpibSession.FormattedIO.Scanf<double>("%e", out double result);
            return result;
        }

        private void SRQHandler(object sender, Ivi.Visa.VisaEventArgs e)
        {
            /* Status Byte Bit Values
             * Bit 7 - Change in Sweep Parameters
             * Bit 6 - Asserted whenever there is an SRQ
             * Bit 5 - Entry Error
             * Bit 4 - End of Sweep
             * Bit 3 - Source Settled
             * Bit 2 - Change in Extended Status Byte
             * Bit 1 - Front Panel Entry Complete
             * Bit 0 - Front Panel Key Pressed
             */

            // Read the Status Byte but discard for now
            var gbs = (GpibSession)sender;
            StatusByteFlags sb = gbs.ReadStatusByte();

            Debug.WriteLine(sb.ToString(), "Status Byte: ");

            // Clear the SRQ event
            gpibSession.DiscardEvents(EventType.ServiceRequest);

            // Assume Data Ready and release the semaphore for now
            srqWait.Release();
        }

        /// <summary>
        /// Releases all resources used by the Device.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the Device and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    try
                    {
                        // Unsubscribe Service Request handler before disposing the GPIB session
                        if (gpibSession != null)
                        {
                            gpibSession.ServiceRequest -= SRQHandler;
                            gpibSession.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error disposing GPIB session: {ex.Message}");
                    }

                    try
                    {
                        resManager?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error disposing Resource Manager: {ex.Message}");
                    }

                    try
                    {
                        srqWait?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error disposing SRQ semaphore: {ex.Message}");
                    }
                }

                disposed = true;
            }
        }
    }
}
