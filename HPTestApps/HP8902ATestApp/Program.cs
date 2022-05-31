using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using HPDevices.HP8902A;

namespace HP8902ATestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // HP 11792A Table
            List<CalibrationFactor> calibrationFactors92A = new List<CalibrationFactor>();

            calibrationFactors92A.Add(new CalibrationFactor(0.05, 100.0));
            calibrationFactors92A.Add(new CalibrationFactor(2.0, 96.3));
            calibrationFactors92A.Add(new CalibrationFactor(3.0, 94.8));
            calibrationFactors92A.Add(new CalibrationFactor(4.0, 93.9));
            calibrationFactors92A.Add(new CalibrationFactor(5.0, 92.9));
            calibrationFactors92A.Add(new CalibrationFactor(6.0, 91.9));
            calibrationFactors92A.Add(new CalibrationFactor(7.0, 91.1));
            calibrationFactors92A.Add(new CalibrationFactor(8.0, 90.3));
            calibrationFactors92A.Add(new CalibrationFactor(9.0, 89.3));
            calibrationFactors92A.Add(new CalibrationFactor(10.0, 88.5));
            calibrationFactors92A.Add(new CalibrationFactor(11.0, 87.5));
            calibrationFactors92A.Add(new CalibrationFactor(12.4, 87.0));
            calibrationFactors92A.Add(new CalibrationFactor(13.0, 86.1));
            calibrationFactors92A.Add(new CalibrationFactor(14.0, 85.6));
            calibrationFactors92A.Add(new CalibrationFactor(15.0, 85.4));
            calibrationFactors92A.Add(new CalibrationFactor(16.0, 84.9));
            calibrationFactors92A.Add(new CalibrationFactor(17.0, 84.6));
            calibrationFactors92A.Add(new CalibrationFactor(18.0, 84.1));

            string fileName = "CalFactors92A.json";
            string jsonString = JsonSerializer.Serialize(calibrationFactors92A);
            File.WriteAllText(fileName, jsonString);

            // HP 11792A Table
            List<CalibrationFactor> calibrationFactors22A = new List<CalibrationFactor>();

            calibrationFactors22A.Add(new CalibrationFactor(0.05, 98.0));
            calibrationFactors22A.Add(new CalibrationFactor(0.0001, 96.8));
            calibrationFactors22A.Add(new CalibrationFactor(0.0003, 99.0));
            calibrationFactors22A.Add(new CalibrationFactor(0.0005, 99.2));
            calibrationFactors22A.Add(new CalibrationFactor(0.001, 99.2));
            calibrationFactors22A.Add(new CalibrationFactor(0.003, 99.1));
            calibrationFactors22A.Add(new CalibrationFactor(0.005, 99.1));
            calibrationFactors22A.Add(new CalibrationFactor(0.01, 98.3));
            calibrationFactors22A.Add(new CalibrationFactor(0.03, 98.0));
            calibrationFactors22A.Add(new CalibrationFactor(0.1, 97.4));
            calibrationFactors22A.Add(new CalibrationFactor(0.3, 95.8));
            calibrationFactors22A.Add(new CalibrationFactor(0.5, 94.8));
            calibrationFactors22A.Add(new CalibrationFactor(1.0, 92.8));
            calibrationFactors22A.Add(new CalibrationFactor(1.5, 91.3));
            calibrationFactors22A.Add(new CalibrationFactor(2.0, 89.9));
            calibrationFactors22A.Add(new CalibrationFactor(2.6, 87.8));

            fileName = "CalFactors22A.json";
            jsonString = JsonSerializer.Serialize(calibrationFactors22A);
            File.WriteAllText(fileName, jsonString);

            
            HPDevices.HP8902A.Device measuringReceiver = new Device(@"GPIB0::14::INSTR");

            // Frequency Tests
            //Console.WriteLine("Frequency value {0}Hz", measuringReceiver.MeasureFrequency());

            //Console.WriteLine("Frequency error {0}Hz", measuringReceiver.MeasureFrequencyError(10000000));

            //Console.WriteLine("AM Modulation {0}%", measuringReceiver.MeasureAMModulationPercent());
            
            //Console.WriteLine("FM Modulation Deviation {0}Hz", measuringReceiver.MeasureFMModulationDeviation());

            //Console.WriteLine("Phase Modulation Angle {0}Rad", measuringReceiver.MeasurePhaseModulationRadian());
            //Console.WriteLine("Phase Modulation Angle {0}Deg", measuringReceiver.MeasurePhaseModulationDegree());

            //Console.WriteLine("Modulation Frequency {0}Hz", measuringReceiver.MeasureModulationFrequency());

            // RF Power Tests
            Console.WriteLine("Loading Calibration Factors");

            Console.WriteLine("Frequency Offset Table");
            measuringReceiver.LoadCalibrationFactors("CalFactors92A.json", true);

            Console.WriteLine("Base Table");
            measuringReceiver.LoadCalibrationFactors("CalFactors22A.json", false);

            // Zero the sensor
            Console.WriteLine("Zeroing Sensor");
            measuringReceiver.ZeroPowerSensor();

            // Calibrate the power sensor
            Console.WriteLine("Calibrating Sensor");
            measuringReceiver.CalibratePowerSensor();

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
