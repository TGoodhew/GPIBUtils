using NationalInstruments.Visa;
using System.Threading;

namespace HPDevices.HP53131A
{
    /// <summary>
    /// Represents an HP 53131A Universal Counter that can measure frequency, period, and time interval.
    /// </summary>
    /// <remarks>
    /// The HP 53131A is a 225 MHz universal counter with three input channels. This class provides
    /// methods to measure frequency on any channel and configure input impedance (50 ohm or 1 Mohm).
    /// Communication is via GPIB with SRQ-based synchronization for measurement completion.
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
        /// Initializes a new instance of the HP 53131A device and establishes GPIB communication.
        /// </summary>
        /// <param name="GPIBAddress">The GPIB address in the format "GPIB0::XX::INSTR" where XX is the device address.</param>
        /// <remarks>
        /// Upon initialization, the device is reset and all status registers are cleared.
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

            // Reset the counter
            SendCommand("*RST");
            SendCommand("*CLS");
            SendCommand("*SRE 0");
            SendCommand("*ESE 0");
            SendCommand(":STAT:PRES");
        }

        /// <summary>
        /// Measures the frequency on the specified input channel.
        /// </summary>
        /// <param name="channel">The input channel number (1, 2, or 3).</param>
        /// <returns>The measured frequency in Hz, or 0 if a timeout occurs.</returns>
        /// <remarks>
        /// This method configures the counter for frequency measurement on the specified channel,
        /// triggers a measurement, and waits for completion using SRQ. If the signal is too low
        /// or missing, the method may timeout and return 0.
        /// </remarks>
        public double MeasureFrequency(int channel)
        {
            double result;

            // Configure the meter for a data ready SRQ
            SendCommand(@"*ESE 1");
            SendCommand(@"*SRE 32");

            // Set the channel
            switch (channel)
            {
                case 1:
                    SendCommand(@"CONF:FREQ (@1)");
                    break;
                case 2:
                    SendCommand(@"CONF:FREQ (@2)");
                    break;
                case 3:
                    SendCommand(@"CONF:FREQ (@3)");
                    break;
                default:
                    break;
            }

            // Read the data
            SendCommand(@":INIT;*OPC");

            // Wait for the read to complete
            srqWait.Wait();

            // Get the data
            SendCommand(@"Fetch?");
            result = ReadSciValue();

            // Clear the SRQ setup
            SendCommand(@"*ESE 0");
            SendCommand(@"*SRE 0");

            return result;
        }

        /// <summary>
        /// Sets the input impedance for the counter.
        /// </summary>
        /// <param name="set50Ohm">True to set 50 ohm impedance; false to set 1 Mohm (high impedance). Default is true.</param>
        /// <remarks>
        /// Use 50 ohm impedance when measuring signals from 50 ohm sources (typical for RF applications).
        /// Use 1 Mohm (high impedance) when measuring signals from high-impedance sources or to minimize loading.
        /// </remarks>
        public void Set50OhmImpedance(bool set50Ohm = true)
        {
            if (set50Ohm)
                SendCommand(@"INP:IMP 50");
            else
                SendCommand(@"INP:IMP 1E+6");
        }

        private void SendCommand(string command)
        {
            gpibSession.FormattedIO.WriteLine(command);
        }

        private double ReadSciValue()
        {
            double result;

            // With malfunctioning signal sources it may take some time for the counter to get a measurement
            // and if the counter timesout then we want to just handle the timeout exception, clear the bus and
            // continue returning a 0 result
            try
            {
                // The frequency is read in scientific notation as Hz
                gpibSession.FormattedIO.Scanf<double>("%e", out result);
            }
            catch (Ivi.Visa.IOTimeoutException ex)
            {
                // Write the exception to the debug output
                System.Diagnostics.Debug.WriteLine(ex.Message);

                // Clear and return a 0 value
                gpibSession.Clear();
                result = 0L;
            }

            return result;
        }

        private void SRQHandler(object sender, Ivi.Visa.VisaEventArgs e)
        {
            // Read the Status Byte but discard for now
            var statusByte = gpibSession.ReadStatusByte();

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
