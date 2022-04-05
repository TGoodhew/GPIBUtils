using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HP8902A;

namespace HP8902ATestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            HP8902A.Device measuringReceiver = new Device(@"GPIB0::14::INSTR");

            Console.WriteLine("Frequency value {0}", measuringReceiver.MeasureFrequency());

            Console.WriteLine("Frequency error {0}", measuringReceiver.MeasureFrequencyError(123000000));

            Console.WriteLine("AM Modulation {0}", measuringReceiver.MeasureAMModulationPercent());

            Console.WriteLine("AM Modulation Frequency {0}", measuringReceiver.MeasureAMModulationFrequency());

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
