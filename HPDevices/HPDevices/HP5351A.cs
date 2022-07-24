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

namespace HPDevices.HP5351A
{
    [Flags]
    public enum SRQMaskFlags : short
    {
        /* Bit 7 - Always Zero
         * Bit 6 - Asserted whenever there is an SRQ
         * Bit 5 - Power On
         * Bit 4 - Local
         * Bit 3 - Overload
         * Bit 2 - Error
         * Bit 1 - Measurement Complete
         * Bit 0 - Data Ready
        */
        DataReady = 0x01,
        MeasurementComplete = 0x02,
        Error = 0x04,
        Overload = 0x08,
        Local = 0x10,
        PowerOne = 0x20,
        SRQAssert = 0x40,
        AlwaysZero = 0x80
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
            SendCommand("SRQMASK,0");

            // Send an instrument preset
            SendCommand("INIT");
        }

        public string GetOvenStatus()
        {
            SendCommand("OVEN?");

            return ReadStringValue();
        }

        public string GetReferenceStatus()
        {
            SendCommand("REF?");

            return ReadStringValue();
        }

        public void SetSampleHold()
        {
            SendCommand("SAMPLE,HOLD");
        }

        public void SetSampleFast()
        {
            SendCommand("SAMPLE,FAST");
        }

        private void SendCommand(string command)
        {
            lastCommand = command;
            gpibSession.FormattedIO.WriteLine(command);
        }

        private string ReadStringValue()
        {
            gpibSession.FormattedIO.Scanf<string>("%s", out string result);

            return result;
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
             * Bit 7 - Always Zero
             * Bit 6 - Asserted whenever there is an SRQ
             * Bit 5 - Power On
             * Bit 4 - Local
             * Bit 3 - Overload
             * Bit 2 - Error
             * Bit 1 - Measurement Complete
             * Bit 0 - Data Ready
            */

            // Read the Status Byte but discard for now
            // TODO: Apply the same solution once the 8673B issue is worked out

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
