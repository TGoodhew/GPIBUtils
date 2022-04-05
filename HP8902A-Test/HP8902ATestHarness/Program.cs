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
            double returnValue;

            HP8902A.Device measuringReceiver = new Device(@"GPIB0::14::INSTR");

            returnValue = measuringReceiver.MeasureFrequency();

            Console.WriteLine("Frequency value {0}",returnValue);

            returnValue = measuringReceiver.MeasureFrequencyError(123000000);
            Console.WriteLine("Frequency error {0}",returnValue);

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
