using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NationalInstruments.Visa;

namespace HP8673B
{
    [Flags]
    public enum SRQMaskFlags : short
    {
        /* Status Byte Bit Values
         * Bit 7 - Change In Sweep Parameters
         * Bit 6 - Asserted whenever there is an SRQ
         * Bit 5 - Entry Error
         * Bit 4 - End Of Sweep
         * Bit 3 - Source Settled
         * Bit 2 - Change In Extended Status
         * Bit 1 - Front Panel Entry Complete
         * Bit 0 - Front Panel Key Pressed
        */
        FrontPanelKey = 0x01,
        FrontPanelComplete = 0x02,
        ChangeExtendedStatus = 0x04,
        SourceSettled = 0x08,
        EndOfSweep = 0x10,
        EntryError = 0x20,
        ChangedSweepParemeters = 0x80
    }

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

            // Send an instrument preset
            SendCommand("IP");
        }

        private void SendCommand(string command)
        {
            gpibSession.FormattedIO.WriteLine(command);
        }
        
        private void SRQHandler(object sender, Ivi.Visa.VisaEventArgs e)
        {
            /* Status Byte Bit Values
             * Bit 7 - Change In Sweep Parameters
             * Bit 6 - Asserted whenever there is an SRQ
             * Bit 5 - Entry Error
             * Bit 4 - End Of Sweep
             * Bit 3 - Source Settled
             * Bit 2 - Change In Extended Status
             * Bit 1 - Front Panel Entry Complete
             * Bit 0 - Front Panel Key Pressed
            */

            // Read the Status Byte but discard for now
            _ = gpibSession.ReadStatusByte();

            // Assume Data Ready and release the semaphore for now
            srqWait.Release();
        }

        ~Device()
        {
            resManager.Dispose();
        }
    }
}
