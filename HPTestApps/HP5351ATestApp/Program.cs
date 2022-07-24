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

            // Start the test process
            Output.Heading("HP 5351A Test Application");
            Output.Information("This app tests basic features of the HP 5351A device class");

            // Check the Oven status
            Output.Prompt("Oven status is: " + frequencyCounter.GetOvenStatus());

            // Check the Reference status
            Output.Prompt("Reference status is: " + frequencyCounter.GetReferenceStatus());

            // Set sample to Hold
            frequencyCounter.SetSampleHold();
            Output.Prompt("Confirm that the sample hold is on");
            
            // Pause before exiting
            Output.Prompt("Exiting");

        }
    }
}
