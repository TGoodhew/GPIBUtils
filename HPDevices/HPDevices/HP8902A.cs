using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ivi.Visa.FormattedIO;
using NationalInstruments.Visa;
using Ivi.Visa;
using System.IO;
using System.Text.Json;

namespace HPDevices.HP8902A
{
    /// <summary>
    /// Service Request (SRQ) mask flags for the HP 8902A Measuring Receiver.
    /// </summary>
    /// <remarks>
    /// These flags control which conditions generate a Service Request on the GPIB bus.
    /// Multiple flags can be combined using bitwise OR operations.
    /// </remarks>
    [Flags]
    public enum SRQMaskFlags : short
    {
        /* Bit 7 - Always 0
         * Bit 6 - Asserted whenever there is an SRQ
         * Bit 5 - Re/Uncalibrated
         * Bit 4 - Frequency Offset Mode Changed
         * Bit 3 - Limit Exceeded
         * Bit 2 - Instrument Error
         * Bit 1 - HP-IB Code Error (always set in the SRQ Mask)
         * Bit 0 - Data Ready
        */
        /// <summary>
        /// Indicates that measurement data is ready to be read.
        /// </summary>
        DataReady = 0x01,
        /// <summary>
        /// Indicates that an instrument error has occurred.
        /// </summary>
        InstrumentError = 0x04,
        /// <summary>
        /// Indicates that a measurement limit has been exceeded.
        /// </summary>
        LimitExceeded = 0x08,
        /// <summary>
        /// Indicates that the frequency offset mode has changed.
        /// </summary>
        FrequencyOffsetModeChanged = 0x10,
        /// <summary>
        /// Indicates that the instrument has become uncalibrated or has been recalibrated.
        /// </summary>
        UnOrRecalibrated = 0x20
    }

    /// <summary>
    /// Represents a calibration factor entry for RF power measurements at a specific frequency.
    /// </summary>
    /// <remarks>
    /// Calibration factors compensate for losses or gains in the measurement path and are
    /// frequency-dependent. They are typically loaded from a JSON file and applied to the
    /// HP 8902A's internal calibration table.
    /// </remarks>
    public class CalibrationFactor
    {
        /// <summary>
        /// Gets or sets the frequency in MHz for which this calibration factor applies.
        /// </summary>
        public decimal Frequency { get; set; }
        
        /// <summary>
        /// Gets or sets the calibration factor in dB.
        /// </summary>
        public decimal CalFactor { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalibrationFactor"/> class with default values.
        /// </summary>
        public CalibrationFactor() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalibrationFactor"/> class with specified values.
        /// </summary>
        /// <param name="frequency">The frequency in MHz.</param>
        /// <param name="calFactor">The calibration factor in dB.</param>
        public CalibrationFactor(double frequency, double calFactor)
        {
            Frequency = (decimal)frequency;
            CalFactor = (decimal)calFactor;
        }
    }

    /// <summary>
    /// Represents an HP 8902A Measuring Receiver that can measure RF signal parameters including
    /// frequency, power, AM modulation, FM modulation, and phase modulation.
    /// </summary>
    /// <remarks>
    /// The HP 8902A is a versatile measuring receiver capable of characterizing RF signals.
    /// It supports calibration factor tables for accurate power measurements and can measure
    /// various modulation parameters. Communication is via GPIB with SRQ-based synchronization.
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

        private List<CalibrationFactor> CalFactors;

        /// <summary>
        /// Initializes a new instance of the HP 8902A device and establishes GPIB communication.
        /// </summary>
        /// <param name="GPIBAddress">The GPIB address in the format "GPIB0::XX::INSTR" where XX is the device address.</param>
        /// <remarks>
        /// Upon initialization, the device is preset to a known state using the IP (Instrument Preset) command.
        /// The timeout is set to 20 seconds to accommodate measurements with 1 Hz resolution.
        /// </remarks>
        public Device (string GPIBAddress)
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

        public double MeasureMWFrequency()
        {
            // Set to frequency mode (M5) and trigger hold (T1)
            // Set LO mode (SP27.3 ideal LO frequency is 120.53MHz)
            // Measure frequency
            // Exit LO mode (SP27.0)

            return 0;
        }
        /// <summary>
        /// Measures the phase modulation in radians.
        /// </summary>
        /// <returns>The measured phase modulation in radians.</returns>
        /// <remarks>
        /// This method configures the instrument for phase modulation measurement (M3 mode),
        /// sets 1 Hz frequency resolution, and triggers a measurement with settling time.
        /// It uses SRQ to wait for measurement completion.
        /// </remarks>
        public double MeasurePhaseModulationRadian()
        {
            // Set to Frequency Mode (M5),Auto-Tuning (AT) and Trigger Hold (T1)
            SendCommand("M3ATT1");

            // Set the RF Frequency Resoloution to 1 HZ (SP7.4)
            SendCommand("7.4SP");

            // Enable SRQ to wait till data is complete (SP22.3)
            SendCommand("22.3SP");

            // Trigger measurement with settling (T3)
            SendCommand("T3");

            // Wait for the data to be available
            srqWait.Wait();

            // Clear the SRQ Mask
            SendCommand("22.0SP");

            return ReadSciValue();
        }

        /// <summary>
        /// Measures the phase modulation in degrees.
        /// </summary>
        /// <returns>The measured phase modulation in degrees.</returns>
        /// <remarks>
        /// This method configures the instrument for phase modulation measurement (M3 mode),
        /// sets 1 Hz frequency resolution, applies a 1.745 scaling factor to convert radians to degrees,
        /// and triggers a measurement with settling time. It uses SRQ to wait for measurement completion.
        /// </remarks>
        public double MeasurePhaseModulationDegree()
        {
            // Set to Frequency Mode (M5),Auto-Tuning (AT) and Trigger Hold (T1)
            SendCommand("M3ATT1");

            // Set the RF Frequency Resoloution to 1 HZ (SP7.4)
            SendCommand("7.4SP");

            // Enable SRQ to wait till data is complete (SP22.3)
            SendCommand("22.3SP");

            // Scale reading by 1.745 to get degrees
            SendCommand("1.745R1");

            // Trigger measurement with settling (T3)
            SendCommand("T3");

            // Wait for the data to be available
            srqWait.Wait();

            // Clear the SRQ Mask
            SendCommand("22.0SP");

            return ReadSciValue();
        }


        /// <summary>
        /// Measures the FM (Frequency Modulation) deviation in Hz.
        /// </summary>
        /// <returns>The measured FM deviation in Hz.</returns>
        /// <remarks>
        /// This method configures the instrument for FM deviation measurement (M2 mode),
        /// sets 1 Hz frequency resolution, and triggers a measurement with settling time.
        /// It uses SRQ to wait for measurement completion.
        /// </remarks>
        public double MeasureFMModulationDeviation()
        {
            // Set to Frequency Mode (M5),Auto-Tuning (AT) and Trigger Hold (T1)
            SendCommand("M2ATT1");

            // Set the RF Frequency Resoloution to 1 HZ (SP7.4)
            SendCommand("7.4SP");

            // Enable SRQ to wait till data is complete (SP22.3)
            SendCommand("22.3SP");

            // Trigger measurement with settling (T3)
            SendCommand("T3");

            // Wait for the data to be available
            srqWait.Wait();

            // Clear the SRQ Mask
            SendCommand("22.0SP");

            return ReadSciValue();
        }

        /// <summary>
        /// Measures the AM (Amplitude Modulation) depth as a percentage.
        /// </summary>
        /// <returns>The measured AM modulation depth as a percentage (0-100%).</returns>
        /// <remarks>
        /// This method configures the instrument for AM measurement (M1 mode),
        /// sets 1 Hz frequency resolution, and triggers a measurement with settling time.
        /// It uses SRQ to wait for measurement completion.
        /// </remarks>
        public double MeasureAMModulationPercent()
        {
            // Set to Frequency Mode (M5),Auto-Tuning (AT) and Trigger Hold (T1)
            SendCommand("M1ATT1");

            // Set the RF Frequency Resoloution to 1 HZ (SP7.4)
            SendCommand("7.4SP");

            // Enable SRQ to wait till data is complete (SP22.3)
            SendCommand("22.3SP");

            // Trigger measurement with settling (T3)
            SendCommand("T3");

            // Wait for the data to be available
            srqWait.Wait();

            // Clear the SRQ Mask
            SendCommand("22.0SP");

            return ReadSciValue();
        }

        /// <summary>
        /// Measures the modulation frequency (AM, FM, or PM) in Hz.
        /// </summary>
        /// <returns>The measured modulation frequency in Hz.</returns>
        /// <remarks>
        /// This method configures the instrument for modulation frequency measurement (S1 mode),
        /// sets 1 Hz frequency resolution, and triggers a measurement with settling time.
        /// It uses SRQ to wait for measurement completion.
        /// </remarks>
        public double MeasureModulationFrequency()
        {
            // Set to Frequency Mode (M5),Auto-Tuning (AT) and Trigger Hold (T1)
            SendCommand("S1ATT1");

            // Set the RF Frequency Resoloution to 1 HZ (SP7.4)
            SendCommand("7.4SP");

            // Enable SRQ to wait till data is complete (SP22.3)
            SendCommand("22.3SP");

            // Trigger measurement with settling (T3)
            SendCommand("T3");

            // Wait for the data to be available
            srqWait.Wait();

            // Clear the SRQ Mask
            SendCommand("22.0SP");

            return ReadSciValue();
        }

        /// <summary>
        /// Measures the frequency error relative to a target frequency.
        /// </summary>
        /// <param name="measurementFreq">The target frequency in Hz to compare against.</param>
        /// <returns>The measured frequency error in Hz (actual frequency - target frequency).</returns>
        /// <remarks>
        /// This method configures the instrument for frequency error measurement (S5 mode),
        /// sets the target frequency, uses 1 Hz frequency resolution, and triggers a measurement
        /// with settling time. It uses SRQ to wait for measurement completion.
        /// </remarks>
        public double MeasureFrequencyError(double measurementFreq)
        {
            // Set to Target Frequency (HZ), Frequency Error Mode (S5), and Trigger Hold (T1)
            SendCommand(String.Format("{0}HZS5T1", measurementFreq));

            // Set the RF Frequency Resoloution to 1 HZ (SP7.4)
            SendCommand("7.4SP");

            // Enable SRQ to wait till data is complete (SP22.3)
            SendCommand("22.3SP");

            // Trigger measurement with settling (T3)
            SendCommand("T3");

            // Wait for the data to be available
            srqWait.Wait();

            // Clear the SRQ Mask
            SendCommand("22.0SP");

            return ReadSciValue();
        }

        /// <summary>
        /// Measures the carrier frequency in Hz.
        /// </summary>
        /// <returns>The measured carrier frequency in Hz.</returns>
        /// <remarks>
        /// This method configures the instrument for frequency measurement (M5 mode) with auto-tuning,
        /// sets 1 Hz frequency resolution, and triggers a measurement with settling time.
        /// It uses SRQ to wait for measurement completion.
        /// </remarks>
        public double MeasureFrequency()
        {
            // Set to Frequency Mode (M5),Auto-Tuning (AT) and Trigger Hold (T1)
            SendCommand("M5ATT1");

            // Set the RF Frequency Resoloution to 1 HZ (SP7.4)
            SendCommand("7.4SP");

            // Enable SRQ to wait till data is complete (SP22.3)
            SendCommand("22.3SP");

            // Trigger measurement with settling (T3)
            SendCommand("T3");

            // Wait for the data to be available
            srqWait.Wait();

            // Clear the SRQ Mask
            SendCommand("22.0SP");

            return ReadSciValue();
        }

        /// <summary>
        /// Loads calibration factors from a JSON file into the instrument's calibration table.
        /// </summary>
        /// <param name="fileName">The path to the JSON file containing calibration factors.</param>
        /// <param name="useFrequencyOffsetTable">
        /// If true, loads factors into the frequency offset table; otherwise, uses the normal calibration table.
        /// Default is false.
        /// </param>
        /// <returns>True if the file was loaded successfully; false if the file does not exist.</returns>
        /// <remarks>
        /// The JSON file should contain an array of CalibrationFactor objects with Frequency (MHz) and CalFactor (dB) values.
        /// This method clears the selected table before loading the new factors.
        /// </remarks>
        public bool LoadCalibrationFactors(string fileName, bool useFrequencyOffsetTable = false)
        {
            // Check that the file exists
            if (File.Exists(fileName))
            {
                CalFactors = JsonSerializer.Deserialize<List<CalibrationFactor>>(File.ReadAllText(fileName));

                // Set the device to RF Power mode and with the trigger off (free run)
                SendCommand("M4T0");

                // Select the appropriate table
                if (useFrequencyOffsetTable)
                    SendCommand("27.1SP"); // Enter Frequency Offset Mode
                else
                    SendCommand("27.0SP"); // Exit Frequency Offset Mode

                // Clear the table
                SendCommand("37.9SP");

                // Store the calibration factors
                foreach(CalibrationFactor factor in CalFactors)
                {
                    SendCommand("37.3SP" + String.Format("{0:F2}MZ{1:F2}CF", factor.Frequency * 1000, factor.CalFactor));
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Performs a zero calibration of the power sensor.
        /// </summary>
        /// <returns>True if the zero operation completed successfully.</returns>
        /// <remarks>
        /// This method places the instrument in RF Power mode (M4), initiates the zero procedure,
        /// and waits for completion using SRQ. The power sensor should have no input signal applied
        /// during the zero operation.
        /// </remarks>
        public bool ZeroPowerSensor()
        {
            // Place the unit into RF Power mode and with the trigger off (free run)
            SendCommand("M4T0");

            // Zero the unit
            SendCommand("ZR");
            
            // When a Zero command is sent we should wait for a data item to be ready to confirm that the Zero has been completed (SP22.3)
            SendCommand("22.3SP");

            // Wait for the data to be available
            srqWait.Wait();

            // Clear the SRQ Mask
            SendCommand("22.0SP");

            return true; //TODO: These return codes need to be updated with errors
        }

        /// <summary>
        /// Performs a calibration of the power sensor using the internal 1 MHz reference.
        /// </summary>
        /// <returns>True if the calibration completed successfully.</returns>
        /// <remarks>
        /// This method places the instrument in RF Power mode (M4), turns on the internal calibration source,
        /// waits for the calibration to complete using SRQ, saves the calibration, and turns off the calibration source.
        /// The power sensor should be connected to the calibration output during this operation.
        /// </remarks>
        public bool CalibratePowerSensor()
        {
            // Place the unit into RF Power mode and with the trigger off (free run)
            SendCommand("M4T0");

            // Turn the calibration source on
            SendCommand("C1");

            // Wait for calibration to complete
            SendCommand("22.3SP");

            // Wait for the data to be available
            srqWait.Wait();

            // Clear the SRQ Mask
            SendCommand("22.0SP");

            // Save the calibration
            SendCommand("SC");

            // Turn the calibration source off
            SendCommand("C0");

            return true; //TODO: These return codes need to be updated with errors
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
             * Bit 7 - Always 0
             * Bit 6 - Asserted whenever there is an SRQ
             * Bit 5 - Re/Uncalibrated
             * Bit 4 - Frequency Offset Mode Changed
             * Bit 3 - Limit Exceeded
             * Bit 2 - Instrument Error
             * Bit 1 - HP-IB Code Error (always set in the SRQ Mask)
             * Bit 0 - Data Ready
            */

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
