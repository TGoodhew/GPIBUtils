﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HPDevices.HP8673B;

namespace HP8673BTestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Device signalGenerator = new Device(@"GPIB0::19::INSTR");

            // Setup the console
            Console.ForegroundColor = ConsoleColor.White;
            Console.BufferHeight = 500;

            signalGenerator.EnableRFOutput(false);

            Prompt("Power Off");

            signalGenerator.EnableRFOutput(true);

            Prompt("Power On");

            var currentFrequency = signalGenerator.SetCWFrequency(16000000000);

            Prompt("Actual Frequency: " + currentFrequency);

            signalGenerator.SetPowerLevel(-56.78);

            Prompt("Power set: -56.78");

            signalGenerator.SetPowerLevel(-16.78);

            Prompt("Power set: -16.78");

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
