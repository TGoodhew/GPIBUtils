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
        }
    }
}
