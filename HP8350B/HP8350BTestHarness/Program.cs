using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using HP8350B;
using HP53131A;
using HPE4418B;
using System.IO;

namespace HP8350BTestHarness
{
    internal class Program
    {
        static void Main(string[] args)
        {
            HP8350B.Device signalGenerator = new HP8350B.Device(@"GPIB0::19::INSTR");
            HP53131A.Device frequencyCounter = new HP53131A.Device(@"GPIB0::23::INSTR");
            HPE4418B.Device powerMeter = new HPE4418B.Device(@"GPIB0::13::INSTR");

            // Setup the console
            Console.ForegroundColor = ConsoleColor.White;
            Console.BufferHeight = 500;

            // Delete the default results file
            if (File.Exists("Results.csv"))
                File.Delete("Results.csv");

            // Set the initial power level at 0 dBm
            signalGenerator.SetPowerLevel(0L);

            // Set initial frequency meter impedance to 50 ohms
            frequencyCounter.Set50OhmImpedance(true);

            // Zero the power meter
            Prompt("Connect the power sensor to the reference output.\nPress any key to continue");

            powerMeter.ZeroAndCalibrateSensor();

            // Connect the power meter to the test setup
            Prompt("Connect the power sensor to the DUT and the counter to Channel 1.\nPress any key to continue");

            // Loop through sub 225 MHz frequencies from 10 MHz in 5MHz steps
            for (int frequency = 10; frequency < 225; frequency += 5)
            {
                Measure(signalGenerator, frequencyCounter, powerMeter, frequency, 1, true);
            }

            // Change channel to 3
            Prompt("Connect the signal generator to channel 3.\nPress any key to continue");

            // Loop through 225 MHz and above frequencies till 2.4GHz in 5MHz steps
            for (int frequency = 1225; frequency < 2405; frequency += 5)
            {
                Measure(signalGenerator, frequencyCounter, powerMeter, frequency, 3, true);
            }

            Prompt("Test completed.\nPress any key to exit.");
        }

        private static void Prompt(string message)
        {
            // Set the console color to green
            Console.ForegroundColor = ConsoleColor.Green;

            // Write the message, beep and wait for a keypress
            Console.WriteLine("\n"+message+"\n");
            Console.Beep();
            Console.ReadKey();

            // Reset the console color
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void Measure(HP8350B.Device signalGenerator, HP53131A.Device frequencyCounter, HPE4418B.Device powerMeter, int frequency, int channel, bool saveToCSV = false, string csvFileName = "results.csv")
        {
            // CW frequency is in Hz
            long totalFreq = frequency * 1000000L;

            //Set the signal generator frequency
            signalGenerator.SetCWFrequency(totalFreq);

            // Measure the factual frequency and power
            var measuredFreq = frequencyCounter.MeasureFrequency(channel);
            var measuredPower = powerMeter.MeasurePower(frequency);

            // If the frequency is 0 then we had a frequency timeout error so set the write color to red
            if (measuredFreq == 0)
                Console.ForegroundColor = ConsoleColor.Red;

            // Display the results
            Console.WriteLine("Set Frequency is {0, -15} \tActual frequency is {1, -15} \tPower is {2}",
                ToEngineeringFormat.Convert(totalFreq, 4, "Hz"),
                ToEngineeringFormat.Convert(measuredFreq, 3, "Hz", true),
                ToEngineeringFormat.Convert(measuredPower, 3, "dBm", true));

            // Reset the write color to white
            Console.ForegroundColor = ConsoleColor.White;

            // Save the results to a CSV file
            if (saveToCSV)
            {
                StreamWriter file = new StreamWriter(csvFileName, append: true);
                file.WriteLine(String.Format("{0},{1},{2}", totalFreq, measuredFreq, measuredPower));
                file.Close();
            }
            return;
        }
    }
}
