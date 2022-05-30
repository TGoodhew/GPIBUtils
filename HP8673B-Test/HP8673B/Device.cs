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

namespace HP8673B
{
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
        FrontPanelKeyPressed = 0x01,
        FrontPanelEntryComplete = 0x02,
        ChangeInESB = 0x04,
        SourceSettled = 0x08,
        EndOfSweep = 0x10,
        EntryError = 0x20,
        SRQAssert = 0x40,
        ChangedSweepParameters = 0x80
    }

    public class Device
    {
        public string gpibAddress { get; }

        private GpibSession gpibSession;
        private ResourceManager resManager;
        private SemaphoreSlim srqWait = new SemaphoreSlim(0, 1); // use a semaphore to wait for the SRQ events

        private string lastCommand;

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

        public void SetPowerLevel(double power)
        {
            // Set the power level (LE)
            SendCommand(String.Format("LE{0}DM", power));
        }

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
            // TODO: Investigate why this pattern seems to work but using gpibSession from the class fails for the 8673B

            /* Background:
             * I was having an issue with the system hanging on the srqWait.Wait() command as the count appeared to get
             * decreased by a "phantom" SRQ that would get handled. It didn't matter if I rebooted my machine or power cycled
             * the 8673B. So I set it aside since the last commit and got back to it today (5/30/2022). Searching for insight
             * I found this pattern of using the GpibSession object passed in via sender rather than using the session stored
             * as a class member. Testing this seems to work but it is unclear to me if this is a just voodoo or if this is the
             * correct pattern to use. The next step is to go back and see if I can recreate the original experience as 
             * using the class member GpibSession worked for my other instruments.
             */
            
            var gbs = (GpibSession)sender;
            StatusByteFlags sb = gbs.ReadStatusByte();

            Debug.WriteLine(sb.ToString(), "Status Byte: ");
            
            // Assume Data Ready and release the semaphore for now
            srqWait.Release();
        }

        ~Device()
        {
            resManager.Dispose();
        }
    }
}
