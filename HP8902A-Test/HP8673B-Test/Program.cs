using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HP8673B;

namespace HP8673B_Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Device signalGenerator = new Device(@"GPIB0::19::INSTR");

            // Setup the console
            Console.ForegroundColor = ConsoleColor.White;
            Console.BufferHeight = 500;

            var currentFrequency = signalGenerator.SetCWFrequency(16000000000);

            Prompt("Actual Frequency: " + currentFrequency);

            signalGenerator.SetPowerLevel(-56.78);

            signalGenerator.SetPowerLevel(-16.78);

            currentFrequency = signalGenerator.SetCWFrequency(19500000000);

            Prompt("Actual Frequency: " + currentFrequency);
        }

        private static void Prompt(string message)
        {
            // Set the console color to green
            Console.ForegroundColor = ConsoleColor.Green;

            // Write the message, beep and wait for a keypress
            Console.WriteLine("\n" + message + "\n");
            Console.Beep();
            Console.ReadKey();

            // Reset the console color
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
