using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ivi.Visa.FormattedIO;
using NationalInstruments.Visa;

namespace HP8902A
{
    public class Device
    {
        public string gpibAddress { get; }

        private GpibSession gpibSession;
        private ResourceManager resManager;
        private SemaphoreSlim srqWait = new SemaphoreSlim(0, 1); // use a semaphore to wait for the SRQ events

        public Device (string GPIBAddress)
        {
            resManager = new ResourceManager();
            gpibAddress = GPIBAddress;

            gpibSession = (GpibSession)resManager.Open(gpibAddress);
            gpibSession.TerminationCharacterEnabled = true;

            gpibSession.Clear();

            // Send an instrument preset
            SendCommand("IP");
        }

        private void SendCommand(string command)
        {
            gpibSession.FormattedIO.WriteLine(command);
        }

        public double MeasureMWFrequency()
        {
            // Set to frequency mode (M5) and trigger hold (T1)
            // Set LO mode (SP27.3 ideal LO frequency is 120.53MHz)
            // Measure frequency
            // Exit LO mode (SP27.0)

            return 0;
        }

        public double MeasureFrequency()
        {
            // Set to frequnecy mode (M5) and trigger hold (T1)
            SendCommand("M5T1");

            // Enable SRQ to wait till data is complete (SP22.3)
            SendCommand("22.3SP");
            gpibSession.ServiceRequest += SRQDataReady;

            // Trigger measurement with settling (T3)
            SendCommand("T3");

            // Wait for the data to be available
            srqWait.Wait();

            // Clear the SRQ Mask and EventHandler
            SendCommand("22.0SP");
            gpibSession.ServiceRequest -= SRQDataReady;

            return ReadFrequency();
        }

        private double ReadFrequency()
        {
            // The frequency is read in engineering notation as Hz
            gpibSession.FormattedIO.Scanf<double>("%e", out double result);
            return result;
        }

        private void SRQDataReady(object sender, Ivi.Visa.VisaEventArgs e)
        {
            srqWait.Release();
        }

        ~Device()
        {
            resManager.Dispose();
        }
    }
}
