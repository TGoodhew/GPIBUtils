using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ivi.Visa.Interop;
using System.IO;

namespace ConsoleApplication1
{
    public struct Measurement
    {
        public int MeasurementNumber;
        public double THD;
        public double Harmonic2;
        public double Harmonic3;
        public double Harmonic4;
        public DateTime MeasurementDateTime;

        public Measurement(int MeasurementNumber, double THD, double Harmonic2, double Harmonic3, double Harmonic4, DateTime MeasurementDateTime)
        {
            this.MeasurementNumber = MeasurementNumber;
            this.THD = THD;
            this.Harmonic2 = Harmonic2;
            this.Harmonic3 = Harmonic3;
            this.Harmonic4 = Harmonic4;
            this.MeasurementDateTime = MeasurementDateTime;
        }
    }

    enum AmplitudeCalibration
    {
        On,
        Off,
    }

    class Program
    {

        static void Main(string[] args)
        {
            // Setup variables
            string SigGenAddress = @"GPIB0::10::INSTR";
            string THDMeterAddress = @"GPIB0::1::INSTR";
            ResourceManager ResMgr = new ResourceManager();
            FormattedIO488 THDMeter = new FormattedIO488();
            FormattedIO488 SigGen = new FormattedIO488();
            AmplitudeCalibration AmpCal = AmplitudeCalibration.Off;
            bool ACUnset = true;
            int NumMeasurements = 0;

            // Create the datafile
            StreamWriter ReportFile = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\3325BHarmonicDistortion.csv");

            // Setup the VISA connection for the Keithley 2015
            THDMeter.IO = (IMessage)ResMgr.Open(THDMeterAddress, AccessMode.NO_LOCK, 2000, null);
            THDMeter.IO.TerminationCharacterEnabled = true;
            THDMeter.IO.Timeout = 20000;

            // Setup the VISA connection for the HP 3325B
            SigGen.IO = (IMessage)ResMgr.Open(SigGenAddress, AccessMode.NO_LOCK, 2000, null);
            SigGen.IO.TerminationCharacterEnabled = true;
            SigGen.IO.Timeout = 20000;

            // Initial HP 3325B settings and get the IDN to confirm its connected
            SigGen.IO.Clear();
            SigGen.WriteString("*RST;", true);
            SigGen.WriteString("FU1;", true);
            SigGen.WriteString("FR100HZ;", true);
            SigGen.WriteString("AM999MV", true);
            SigGen.WriteString("*IDN?;", true);
            System.Threading.Thread.Sleep(1000);
            string temp = SigGen.ReadString();
            Console.WriteLine("3325B ID is: {0}", temp);

            // Initial Keithley Settings and get IDN to confirm its connected
            THDMeter.IO.Clear();
            THDMeter.WriteString("*RST;", true);
            THDMeter.WriteString("*IDN?;", true);
            System.Threading.Thread.Sleep(1000);
            temp = THDMeter.ReadString();
            Console.WriteLine("2015THD Meter ID is: {0}", temp);

            // Request amplitude calibration setting
            Console.WriteLine("The 3325B amplitude can drift over time.\n\nPress 1 to turn calibration ON.\nPress 2 to turn calibration OFF.");

            do
            {
                ConsoleKeyInfo key = Console.ReadKey();

                switch (key.KeyChar)
                {
                    case '1':
                        AmpCal = AmplitudeCalibration.On;
                        ACUnset = false;
                        break;
                    case '2':
                        AmpCal = AmplitudeCalibration.Off;
                        ACUnset = false;
                        break;
                }
            } while (ACUnset == true);

            // Get the number of measurements
            Console.WriteLine("\n\nEnter the number of measurements (AC On takes ~12s, AC Off takes ~10s)");

            var loops = Console.ReadLine();
            while (!int.TryParse(loops, out NumMeasurements))
            {
                Console.WriteLine("Please enter an integer number (for example 123)");
                loops = Console.ReadLine();
            }

            // Setup for THD reading
            THDMeter.WriteString(@":INIT:CONT OFF;", true);
            THDMeter.WriteString(@":SENSe:FUNCtion 'DISTortion';", true);
            THDMeter.WriteString(@":SENSe:DISTortion:TYPE THD;", true);
            THDMeter.WriteString(@":SENSe:DISTortion:FREQuency:AUTO ON;", true);
            THDMeter.WriteString(@":SENSe:DISTortion:HARMonic 04;", true);
            THDMeter.WriteString(@":UNIT:DISTortion DB;", true);
            THDMeter.WriteString(@":SENSe:DISTortion:SFILter NONE;", true);
            THDMeter.WriteString(@":SENSe:DISTortion:RANGe:AUTO ON;", true);

            // Create report header
            Console.WriteLine("\n\n{0,14}{1,14}{2,14}{3,14}{4,14}", "Measurement", "THD", "2nd", "3rd", "4th");
            ReportFile.WriteLine("{0},{1},{2},{3},{4},{5}", "Measurement", "Time", "THD", "2nd", "3rd", "4th");

            // Take 10 measurements 
            for (int loopCount = 0; loopCount < NumMeasurements; loopCount++)
            {
                // Take a measurement
                var m = TakeMeasurement(THDMeter, SigGen, loopCount, AmpCal);

                // Write report line
                Console.WriteLine("{0,14}{1,14}{2,14}{3,14}{4,14}", m.MeasurementNumber, m.THD, m.Harmonic2, m.Harmonic3, m.Harmonic4);
                ReportFile.WriteLine("{0},{1},{2},{3},{4},{5}", m.MeasurementNumber, m.MeasurementDateTime.TimeOfDay, m.THD, m.Harmonic2, m.Harmonic3, m.Harmonic4);

            }

            // Close the report file
            ReportFile.Close();

            Console.WriteLine("\nEnter any key to exit");
            Console.ReadKey();
        }

        private static Measurement TakeMeasurement(FormattedIO488 src, FormattedIO488 gen, int measurement, AmplitudeCalibration AmpCal)
        {
            // HP 3325B drifts so Check to see if the user wants amplitude calibration
            if (AmpCal == AmplitudeCalibration.On)
            {
                gen.WriteString("AC;");
                System.Threading.Thread.Sleep(2000);
            }

            // Take a THD reading
            src.WriteString(@":INIT;", true);
            System.Threading.Thread.Sleep(6000);
            src.WriteString(@":SENSe:DISTortion:THD?;", true);
            System.Threading.Thread.Sleep(1000);
            double thd = src.ReadNumber();

            //src.WriteString(@": INIT;");
            src.WriteString(@":SENSe:DISTortion:HARMonic:MAGNitude? 2,2;", true);
            System.Threading.Thread.Sleep(1000);
            double harm2 = src.ReadNumber();
            src.WriteString(@":SENSe:DISTortion:HARMonic:MAGNitude? 3,3;", true);
            System.Threading.Thread.Sleep(1000);
            double harm3 = src.ReadNumber();
            src.WriteString(@":SENSe:DISTortion:HARMonic:MAGNitude? 4,4;", true);
            System.Threading.Thread.Sleep(1000);
            double harm4 = src.ReadNumber();

            return new Measurement(measurement, thd, harm2, harm3, harm4, DateTime.Now);
        }
    }
}
