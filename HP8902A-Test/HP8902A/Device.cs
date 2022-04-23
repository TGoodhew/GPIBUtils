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

namespace HP8902A
{
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
        DataReady = 0x01,
        InstrumentError = 0x04,
        LimitExceeded = 0x08,
        FrequencyOffsetModeChanged = 0x10,
        UnOrRecalibrated = 0x20
    }

    public class CalibrationFactor
    {
        public decimal Frequency { get; set; }
        public decimal CalFactor { get; set; }

        public CalibrationFactor() { }

        public CalibrationFactor(double frequency, double calFactor)
        {
            Frequency = (decimal)frequency;
            CalFactor = (decimal)calFactor;
        }
    }

    public class Device
    {
        public string gpibAddress { get; }

        private GpibSession gpibSession;
        private ResourceManager resManager;
        private SemaphoreSlim srqWait = new SemaphoreSlim(0, 1); // use a semaphore to wait for the SRQ events

        private List<CalibrationFactor> CalFactors;

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

        ~Device()
        {
            resManager.Dispose();
        }
    }
}
