using Ivi.Visa.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace _3325BDCOffsetTest
{
    public enum VoltUnits
    {
        Volts,
        Millivolts
    }

    public enum FreqUnits
    {
        MHz,
        KHz,
        Hz
    }

    public enum Waves
    {
        Sine,
        Square,
        Triangle,
        PosRamp
    }

    public struct VoltageValue
    {
        public float SetPoint;
        public float MinValue;
        public float MaxValue;
        public VoltUnits VoltageUnits;
    }

    public struct VoltageFrequencyValue
    {
        public float SetPoint;
        public float MinValue;
        public float MaxValue;
        public VoltUnits VoltageUnits;
        public double SetFrequency;
        public FreqUnits FrequencyUnits;
        public Waves WaveType;
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
            string PassFailResult;
            //int NumMeasurements = 0;

            // Test Values
            VoltageValue[] TestVoltages =
                new VoltageValue[] {
                    new VoltageValue() { SetPoint = 1.499F, MinValue = 1.493F, MaxValue = 1.50499F, VoltageUnits = VoltUnits.Volts},
                    new VoltageValue() { SetPoint = -1.499F, MinValue = -1.50499F, MaxValue = -1.493F, VoltageUnits = VoltUnits.Volts},
                    new VoltageValue() { SetPoint = 499.9F, MinValue = 497.9F, MaxValue = 501.9F, VoltageUnits = VoltUnits.Millivolts},
                    new VoltageValue() { SetPoint = -499.9F, MinValue = -501.9F, MaxValue = -497.9F, VoltageUnits = VoltUnits.Millivolts},
                    new VoltageValue() { SetPoint = 149.9F, MinValue = 149.3F, MaxValue = 150.5F, VoltageUnits = VoltUnits.Millivolts},
                    new VoltageValue() { SetPoint = -149.9F, MinValue = -150.5F, MaxValue = -149.3F, VoltageUnits = VoltUnits.Millivolts},
                    new VoltageValue() { SetPoint = 49.99F, MinValue = 49.79F, MaxValue = 50.19F, VoltageUnits = VoltUnits.Millivolts},
                    new VoltageValue() { SetPoint = -49.99F, MinValue = -50.19F, MaxValue = -49.79F, VoltageUnits = VoltUnits.Millivolts},
                    new VoltageValue() { SetPoint = 14.99F, MinValue = 14.93F, MaxValue = 15.05F, VoltageUnits = VoltUnits.Millivolts},
                    new VoltageValue() { SetPoint = -14.99F, MinValue = -15.05F, MaxValue = -14.93F, VoltageUnits = VoltUnits.Millivolts},
                    new VoltageValue() { SetPoint = 4.999F, MinValue = 4.979F, MaxValue = 5.019F, VoltageUnits = VoltUnits.Millivolts},
                    new VoltageValue() { SetPoint = -4.999F, MinValue = -5.019F, MaxValue = -4.979F, VoltageUnits = VoltUnits.Millivolts},
                    new VoltageValue() { SetPoint = 1.499F, MinValue = 1.479F, MaxValue = 1.519F, VoltageUnits = VoltUnits.Millivolts},
                    new VoltageValue() { SetPoint = -1.499F, MinValue = -1.519F, MaxValue = -1.479F, VoltageUnits = VoltUnits.Millivolts}
                };

            VoltageFrequencyValue[] TestFreqVoltages =
                new VoltageFrequencyValue[]
                {
                    new VoltageFrequencyValue() {SetPoint = 4.5F, MinValue = 4.350F, MaxValue = 4.650F, VoltageUnits = VoltUnits.Volts, SetFrequency=20.999999999D, FrequencyUnits = FreqUnits.MHz, WaveType = Waves.Sine},
                    new VoltageFrequencyValue() {SetPoint = -4.5F, MinValue = -4.650F, MaxValue = -4.350F, VoltageUnits = VoltUnits.Volts, SetFrequency=20.999999999D, FrequencyUnits = FreqUnits.MHz, WaveType = Waves.Sine},
                    new VoltageFrequencyValue() {SetPoint = 4.5F, MinValue = 4.440F, MaxValue = 4.560F, VoltageUnits = VoltUnits.Volts, SetFrequency=999.9F, FrequencyUnits = FreqUnits.KHz, WaveType = Waves.Sine},
                    new VoltageFrequencyValue() {SetPoint = -4.5F, MinValue = -4.560F, MaxValue = -4.440F, VoltageUnits = VoltUnits.Volts, SetFrequency=999.9F, FrequencyUnits = FreqUnits.KHz, WaveType = Waves.Sine},
                    new VoltageFrequencyValue() {SetPoint = 4.5F, MinValue = 4.440F, MaxValue = 4.560F, VoltageUnits = VoltUnits.Volts, SetFrequency=999.9F, FrequencyUnits = FreqUnits.KHz, WaveType = Waves.Square},
                    new VoltageFrequencyValue() {SetPoint = -4.5F, MinValue = -4.560F, MaxValue = -4.440F, VoltageUnits = VoltUnits.Volts, SetFrequency=999.9F, FrequencyUnits = FreqUnits.KHz, WaveType = Waves.Square},
                    new VoltageFrequencyValue() {SetPoint = -4.5F, MinValue = -4.650F, MaxValue = -4.350F, VoltageUnits = VoltUnits.Volts, SetFrequency=9.999F, FrequencyUnits = FreqUnits.MHz, WaveType = Waves.Square},
                    new VoltageFrequencyValue() {SetPoint = -4.5F, MinValue = -4.560F, MaxValue = -4.440F, VoltageUnits = VoltUnits.Volts, SetFrequency=9.9F, FrequencyUnits = FreqUnits.KHz, WaveType = Waves.Triangle},
                    new VoltageFrequencyValue() {SetPoint = -4.5F, MinValue = -4.620F, MaxValue = -4.380F, VoltageUnits = VoltUnits.Volts, SetFrequency=9.9F, FrequencyUnits = FreqUnits.KHz, WaveType = Waves.PosRamp},
                };

            // Create the datafile
            //StreamWriter ReportFile = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\3325BHarmonicDistortion.csv");

            // Setup the VISA connection for the Keithley 2015
            THDMeter.IO = (IMessage)ResMgr.Open(THDMeterAddress, AccessMode.NO_LOCK, 2000, null);
            THDMeter.IO.TerminationCharacterEnabled = true;
            THDMeter.IO.Timeout = 20000;

            // Setup the VISA connection for the HP 3325B
            SigGen.IO = (IMessage)ResMgr.Open(SigGenAddress, AccessMode.NO_LOCK, 2000, null);
            SigGen.IO.TerminationCharacterEnabled = true;
            SigGen.IO.Timeout = 20000;

            // Initialize the HP 3325B and get the IDN to confirm its connected
            SigGen.IO.Clear();
            SigGen.WriteString("*RST;", true);
            SigGen.WriteString("*IDN?;", true);
            System.Threading.Thread.Sleep(1000);
            string temp = SigGen.ReadString();
            Console.WriteLine("3325B ID is: {0}", temp);

            // Initialize the Keithley and get IDN to confirm its connected
            THDMeter.IO.Clear();
            THDMeter.WriteString("*RST;", true);
            THDMeter.WriteString("*IDN?;", true);
            System.Threading.Thread.Sleep(1000);
            temp = THDMeter.ReadString();
            Console.WriteLine("2015THD ID is: {0}", temp);

            // Configure Keithley for DC voltage measurement
            THDMeter.WriteString(@":INIT:CONT OFF;", true);
            THDMeter.WriteString(@":SENSe:FUNCtion 'VOLTage:DC';", true);
            THDMeter.WriteString(@":SENSe:VOLTage:DC:RANGe:AUTO ON;", true);

            // Configure 3325B for DC Offset Accuracy (DC only) test
            Console.WriteLine("\n\nDC Offset Accuracy (DC Only)\n\n");
            SigGen.WriteString("FU0;", true);

            // Take initial readings to confirm
            double voltage = SetAndTakeSingleMeasurement(THDMeter, SigGen, 5.0, VoltUnits.Volts);

            if ((voltage >= 4.980F) & (voltage <= 5.020F))
                PassFailResult = "Pass";
            else
                PassFailResult = "Fail";

            Console.WriteLine("Step D voltage (4.980  to  5.020): {0, 12} {1}", voltage, PassFailResult);

            voltage = SetAndTakeSingleMeasurement(THDMeter, SigGen, -5.0, VoltUnits.Volts);

            if ((voltage >= -5.020F) & (voltage <= -4.980F))
                PassFailResult = "Pass";
            else
                PassFailResult = "Fail";

            Console.WriteLine("Step D voltage (-4.980 to -5.020): {0, 12} {1}\n\n", voltage, PassFailResult);

            // Attenuator Test
            foreach (VoltageValue val in TestVoltages)
            {
                voltage = SetAndTakeSingleMeasurement(THDMeter, SigGen, val.SetPoint, val.VoltageUnits);

                if ((voltage >= val.MinValue) & (voltage <= val.MaxValue))
                    PassFailResult = "Pass";
                else
                    PassFailResult = "Fail";

                Console.WriteLine("Step F voltage ({0,8} to {1,8}): {2, 12} {3}", val.MinValue, val.MaxValue, voltage, PassFailResult);
            }


            // Configure 3325B for DC Offset Accuracy with AC Functions
            Console.WriteLine("\n\nDC Offset Accuracy with AC Functions Test\n\n");

            foreach (VoltageFrequencyValue val in TestFreqVoltages)
            {
                SetAndTakeSingleFrequencyMeasurement(THDMeter, SigGen, out PassFailResult, out voltage, val);

                Console.WriteLine("Frequency {0,8:F3} {1,3} {2,8} Wave Voltage ({3,6:F3}  to {4,6:F3}): {5, 12:F3} {6}", val.SetFrequency, val.FrequencyUnits, val.WaveType, val.MinValue, val.MaxValue, voltage, PassFailResult);
            }

            //ReportFile.Close();

            Console.WriteLine("\nEnter any key to exit");
            Console.ReadKey();
        }

        private static void SetAndTakeSingleFrequencyMeasurement(FormattedIO488 THDMeter, FormattedIO488 SigGen, out string PassFailResult, out double voltage, VoltageFrequencyValue val)
        {
            switch (val.WaveType)
            {
                case Waves.Sine:
                    SigGen.WriteString("FU1;", true);
                    break;
                case Waves.Square:
                    SigGen.WriteString("FU2;", true);
                    break;
                case Waves.Triangle:
                    SigGen.WriteString("FU3;", true);
                    break;
                case Waves.PosRamp:
                    SigGen.WriteString("FU4;", true);
                    break;
            }

            var FreqStr = string.Format("FR {0:F9} ", val.SetFrequency);

            switch (val.FrequencyUnits)
            {
                case FreqUnits.Hz:
                    FreqStr += "HZ;";
                    break;
                case FreqUnits.KHz:
                    FreqStr += "KH;";
                    break;
                case FreqUnits.MHz:
                    FreqStr += "MH;";
                    break;
            }

            SigGen.WriteString(FreqStr, true);

            SigGen.WriteString("AM 1.0 VO;", true);

            var DCOffsetStr = string.Format("OF {0:F2} VO; AC;", val.SetPoint);
            SigGen.WriteString(DCOffsetStr, true);

            System.Threading.Thread.Sleep(2000);

            THDMeter.WriteString(@":READ?");
            voltage = THDMeter.ReadNumber();

            if ((voltage >= val.MinValue) & (voltage <= val.MaxValue))
                PassFailResult = "Pass";
            else
                PassFailResult = "Fail";
        }

        private static double SetAndTakeSingleMeasurement(FormattedIO488 THDMeter, FormattedIO488 SigGen, double setVoltage, VoltUnits Units)
        {
            string dcOffset;

            if (Units == VoltUnits.Volts)
                dcOffset = string.Format("OF {0:F3} VO; AC; ", setVoltage);
            else
                dcOffset = string.Format("OF {0:F3} MV; AC; ", setVoltage);

            SigGen.WriteString(dcOffset, true);
            System.Threading.Thread.Sleep(1000);
            THDMeter.WriteString(@":READ?");
            double voltage = THDMeter.ReadNumber();

            if (Units == VoltUnits.Millivolts)
                voltage *= 1000;

            return voltage;
        }

    }
}
