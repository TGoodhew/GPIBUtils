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
    /// <summary>
    /// Service Request (SRQ) mask flags for the HP 5351A Frequency Counter.
    /// </summary>
    /// <remarks>
    /// These flags control which conditions generate a Service Request on the GPIB bus.
    /// Multiple flags can be combined using bitwise OR operations.
    /// </remarks>
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
        /// <summary>
        /// Indicates that measurement data is ready to be read.
        /// </summary>
        DataReady = 0x01,
        /// <summary>
        /// Indicates that a measurement has completed.
        /// </summary>
        MeasurementComplete = 0x02,
        /// <summary>
        /// Indicates that an error has occurred.
        /// </summary>
        Error = 0x04,
        /// <summary>
        /// Indicates that an input overload condition exists.
        /// </summary>
        Overload = 0x08,
        /// <summary>
        /// Indicates that the instrument is in local mode.
        /// </summary>
        Local = 0x10,
        /// <summary>
        /// Indicates that the instrument has been powered on.
        /// </summary>
        PowerOne = 0x20,
        /// <summary>
        /// Asserted whenever a Service Request occurs.
        /// </summary>
        SRQAssert = 0x40,
        /// <summary>
        /// Always zero (reserved bit).
        /// </summary>
        AlwaysZero = 0x80
    }

    /// <summary>
    /// Represents an HP 5351A Microwave Frequency Counter with oven-controlled timebase.
    /// </summary>
    /// <remarks>
    /// The HP 5351A is a high-stability frequency counter with an oven-controlled crystal oscillator
    /// for improved frequency reference stability. This class provides methods to query oven and
    /// reference status, and control sampling mode via GPIB communication.
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
        /// Initializes a new instance of the HP 5351A device and establishes GPIB communication.
        /// </summary>
        /// <param name="GPIBAddress">The GPIB address in the format "GPIB0::XX::INSTR" where XX is the device address.</param>
        /// <remarks>
        /// Upon initialization, the SRQ mask is cleared and the device is initialized using the INIT command.
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

            // Ensure the the SRQ mask is 0
            SendCommand("SRQMASK,0");

            // Send an instrument preset
            SendCommand("INIT");
        }

        /// <summary>
        /// Queries the oven status of the internal timebase.
        /// </summary>
        /// <returns>A string indicating the oven status (e.g., "WARM", "READY").</returns>
        /// <remarks>
        /// The oven-controlled crystal oscillator requires time to warm up and stabilize after power-on.
        /// This method queries the OVEN? command to determine if the timebase has reached operating temperature.
        /// </remarks>
        public string GetOvenStatus()
        {
            SendCommand("OVEN?");

            return ReadStringValue();
        }

        /// <summary>
        /// Queries the reference source status.
        /// </summary>
        /// <returns>A string indicating the reference status (e.g., "INT" for internal, "EXT" for external).</returns>
        /// <remarks>
        /// The counter can use either its internal oven-controlled oscillator or an external reference.
        /// This method queries the REF? command to determine which reference is active.
        /// </remarks>
        public string GetReferenceStatus()
        {
            SendCommand("REF?");

            return ReadStringValue();
        }

        /// <summary>
        /// Sets the sampling mode to HOLD, which holds the current measurement result.
        /// </summary>
        /// <remarks>
        /// In HOLD mode, the counter stops taking new measurements and displays the last result.
        /// Use this mode when you need a stable reading for recording or analysis.
        /// </remarks>
        public void SetSampleHold()
        {
            SendCommand("SAMPLE,HOLD");
        }

        /// <summary>
        /// Sets the sampling mode to FAST, which enables continuous rapid measurements.
        /// </summary>
        /// <remarks>
        /// In FAST mode, the counter takes measurements continuously at the fastest possible rate.
        /// Use this mode for real-time frequency monitoring or when maximum measurement speed is needed.
        /// </remarks>
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
