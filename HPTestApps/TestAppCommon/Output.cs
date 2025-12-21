using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAppCommon
{
    public static class Output
    {
        public static void SetupConsole(int bufferHeight = 9999)
        {
            // Setup the console
            Console.ForegroundColor = ConsoleColor.White;
            
            // Set buffer height to prevent log truncation
            // Default is 9999 (near maximum) to allow extensive logging
            // Can be customized by passing a different value
            try
            {
                Console.BufferHeight = bufferHeight;
            }
            catch (ArgumentOutOfRangeException)
            {
                // If the requested buffer height is invalid, use system default
                // This can happen if bufferHeight is too small or exceeds system limits
            }
        }
        public static void Prompt(string message)
        {
            // Set the console color to green
            Console.ForegroundColor = ConsoleColor.Green;

            // Write the message, beep and wait for a keypress
            Console.WriteLine("\n" + message + "\nHit any key to continue\n");
            Console.Beep();
            Console.ReadKey();

            // Reset the console color to the base white
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Heading (string message, bool beep=false)
        {
            // Set the console color to blue
            Console.ForegroundColor = ConsoleColor.Blue;

            // Write the message and beep if requested
            Console.WriteLine("\n" + message + "\n");
            if (beep)
                Console.Beep();

            // Reset the console color to the base white
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Information(string message)
        {
            // Set the console color to white (already base color so no need to reset)
            Console.ForegroundColor = ConsoleColor.White;

            // Write the message
            Console.WriteLine(message + "\n");
        }
    }
}
