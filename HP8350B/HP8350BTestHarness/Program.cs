using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using HP8350B;
using HP53131A;

namespace HP8350BTestHarness
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int step = 1;

            HP8350B.Device signalGenerator = new HP8350B.Device(@"GPIB0::19::INSTR");
            HP53131A.Device frequencyCounter = new HP53131A.Device(@"GPIB0::23::INSTR");

            // Set the initial power level at 0 dBm
            signalGenerator.SetPowerLevel(0L);

            // Set initial frequency meter impedance to 50 ohms
            frequencyCounter.Set50OhmImpedance(true);

            // Loop through sub 225 MHz frequencies from 10 MHz in 5MHz steps
            for (int frequency = 10; frequency < 225; frequency+=5)
            {
                // CW frequency is in Hz
                signalGenerator.SetCWFrequency(frequency * 1000000);

                Console.WriteLine("Step {0} Set Frequency is {1} Actual frequency is {2}", step++, ToEngineeringFormat.Convert(frequency * 1000000, 4, "Hz"), ToEngineeringFormat.Convert(frequencyCounter.MeasureFrequency(1),4,"Hz"));
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
