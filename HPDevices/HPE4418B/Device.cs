using NationalInstruments.Visa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HPDevices.HPE4418B
{
    /// <summary>
    /// Represents an Agilent E4418B Power Meter with dual-channel RF power measurement capability.
    /// </summary>
    /// <remarks>
    /// The E4418B is a single or dual-channel power meter that works with external power sensors.
    /// This class provides methods to zero and calibrate the sensor, and measure RF power at specified
    /// frequencies. Communication is via GPIB with SRQ-based synchronization for operation completion.
    /// </remarks>
    public class Device
    {
        /// <summary>
        /// Gets the GPIB address for this device in the format "GPIB0::XX::INSTR".
        /// </summary>
        public string gpibAddress { get; }

        private GpibSession gpibSession;
        private ResourceManager resManager;
        private SemaphoreSlim srqWait = new SemaphoreSlim(0, 1); // use a semaphore to wait for the SRQ events

        /// <summary>
        /// Initializes a new instance of the E4418B device and establishes GPIB communication.
        /// </summary>
        /// <param name="GPIBAddress">The GPIB address in the format "GPIB0::XX::INSTR" where XX is the device address.</param>
        /// <remarks>
        /// Upon initialization, the device is reset and all status registers and event enables are cleared.
        /// The timeout is set to 20 seconds to accommodate calibration operations.
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

            // Reset the meter
            SendCommand("*RST");
            SendCommand("*CLS");
            SendCommand("*SRE 0");
            SendCommand("*ESE 0");
        }

        /// <summary>
        /// Performs zero and calibration of all connected power sensors.
        /// </summary>
        /// <remarks>
        /// This method initiates a full calibration sequence that includes zeroing (with no input signal)
        /// and calibration (with the internal 50 MHz reference or external cal source). The power sensor
        /// should have no input signal applied during the zero phase. This operation may take several seconds
        /// and uses SRQ to signal completion.
        /// </remarks>
        public void ZeroAndCalibrateSensor()
        {
            // Setup the SRQ mask for an operation complete message (SRE 32 ESE 1)
            SendCommand(@"*ESE 1");
            SendCommand(@"*SRE 32");

            // Initiate a calibration sequence (Zero & Cal)
            SendCommand(@":CAL1:ALL");

            // Tell the unit to signal for operation complete
            SendCommand(@"*OPC");

            // Wait for the read to complete
            srqWait.Wait();

            // Clear the SRQ mask
            SendCommand(@"*SRE 0");
            SendCommand(@"*ESE 0");
        }

        /// <summary>
        /// Measures the RF power at the specified frequency.
        /// </summary>
        /// <param name="frequency">The measurement frequency in MHz. This sets the frequency correction factor for the power sensor.</param>
        /// <returns>The measured power in dBm (decibels relative to 1 milliwatt), or 0 if a timeout occurs.</returns>
        /// <remarks>
        /// This method sets the frequency correction factor, configures the power meter for channel 1,
        /// initiates a measurement, and waits for completion using SRQ. If the input signal is too low
        /// or missing, the method may timeout and return 0. The frequency setting is important for
        /// accurate power measurements as sensor response varies with frequency.
        /// </remarks>
        public double MeasurePower(int frequency)
        {
            double result;

            // Set the measurement frequency
            SendCommand(String.Format(":FREQ {0}MHZ", frequency));

            // Setup the SRQ mask for an operation complete message (SRE 32 ESE 1)
            SendCommand(@"*ESE 1");
            SendCommand(@"*SRE 32");

            // Read the data
            SendCommand(@":CONF1;:INIT;*OPC");

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

        private void SendCommand(string command)
        {
            gpibSession.FormattedIO.WriteLine(command);
        }

        private double ReadSciValue()
        {
            double result;

            // With malfunctioning signal sources it may take some time for the meter to get a measurement
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

        ~Device()
        {
            resManager.Dispose();
        }
    }
}
