using NationalInstruments.Visa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HPE4418B
{
    public class Device
    {
        public string gpibAddress { get; }

        private GpibSession gpibSession;
        private ResourceManager resManager;
        private SemaphoreSlim srqWait = new SemaphoreSlim(0, 1); // use a semaphore to wait for the SRQ events

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
