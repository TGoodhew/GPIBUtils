using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAppCommon
{
    public static class Output
    {
        public static void SetupConsole()
        {
            // Setup the console
            Console.ForegroundColor = ConsoleColor.White;
            Console.BufferHeight = 500;
        }
        public static void Prompt(string message)
        {
            // Set the console color to green
            Console.ForegroundColor = ConsoleColor.Green;

            // Write the message, beep and wait for a keypress
            Console.WriteLine("\n" + message + "\nHit any key to continue\n");
            Console.Beep();
            Console.ReadKey();

            // Reset the console color
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Heading (string message, bool beep=false)
        {
            // Set the console color to green
            Console.ForegroundColor = ConsoleColor.Blue;

            // Write the message and beep if requested
            Console.WriteLine("\n" + message + "\n");
            if (beep)
                Console.Beep();

            // Reset the console color
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Information(string message)
        {
            // Set the console color to green
            Console.ForegroundColor = ConsoleColor.White;

            // Write the message
            Console.WriteLine(message + "\n");
        }
    }
}
