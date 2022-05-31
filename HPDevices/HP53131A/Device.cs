using NationalInstruments.Visa;
using System.Threading;

namespace HPDevices.HP53131A
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

            // Reset the counter
            SendCommand("*RST");
            SendCommand("*CLS");
            SendCommand("*SRE 0");
            SendCommand("*ESE 0");
            SendCommand(":STAT:PRES");
        }

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

        ~Device()
        {
            resManager.Dispose();
        }
    }
}
