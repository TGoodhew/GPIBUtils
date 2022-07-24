using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HPDevices.HP5351A;
using TestAppCommon;

namespace HP5351ATestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Device frequencyCounter = new Device(@"GPIB0::14::INSTR");

            // Setup the console
            Output.SetupConsole();

            // Pause before exiting
            Output.Prompt("Exiting");

        }
    }
}
