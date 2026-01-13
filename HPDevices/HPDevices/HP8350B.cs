using NationalInstruments.Visa;
using System;
using System.Threading;

namespace HPDevices.HP8350B
{
    /// <summary>
    /// Represents an HP 8350B Sweep Oscillator Mainframe that provides control over RF signal generation.
    /// </summary>
    /// <remarks>
    /// The HP 8350B is a sweeper mainframe that works with plug-in modules to generate RF signals.
    /// This class provides methods to set continuous wave (CW) frequency and power level via GPIB communication.
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

        /// <summary>
        /// Initializes a new instance of the HP 8350B device and establishes GPIB communication.
        /// </summary>
        /// <param name="GPIBAddress">The GPIB address in the format "GPIB0::XX::INSTR" where XX is the device address.</param>
        /// <remarks>
        /// Upon initialization, the device is preset to a known state using the IP (Instrument Preset) command.
        /// The timeout is set to 20 seconds to accommodate measurements with 1 Hz resolution.
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

            // Send an instrument preset
            SendCommand("IP");
        }

        /// <summary>
        /// Sets the continuous wave (CW) output frequency of the signal generator.
        /// </summary>
        /// <param name="frequency">The desired frequency in Hertz (Hz).</param>
        /// <remarks>
        /// The frequency is set using the CW command followed by the frequency value in Hz.
        /// Valid frequency range depends on the installed plug-in module.
        /// </remarks>
        public void SetCWFrequency(double frequency)
        {
            // Set the CW frequency in Hz (CW) 
            SendCommand(String.Format("CW{0}HZ", frequency));
        }

        /// <summary>
        /// Sets the output power level of the signal generator.
        /// </summary>
        /// <param name="power">The desired power level in dBm (decibels relative to 1 milliwatt).</param>
        /// <remarks>
        /// The power level is set using the PL command followed by the power value in dBm.
        /// Valid power range depends on the installed plug-in module and frequency.
        /// </remarks>
        public void SetPowerLevel(double power)
        {
            // Set the power level in dBm (PL) 
            SendCommand(String.Format("PL{0}DM", power));
        }

        private void SendCommand(string command)
        {
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
            // Read the Status Byte but discard for now
            _ = gpibSession.ReadStatusByte();

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
                        gpibSession?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disposing GPIB session: {ex.Message}");
                    }

                    try
                    {
                        resManager?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disposing Resource Manager: {ex.Message}");
                    }

                    try
                    {
                        srqWait?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disposing SRQ semaphore: {ex.Message}");
                    }
                }

                disposed = true;
            }
        }
    }
}
