using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToEngineeringFormatRework
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string result;

            result = String.Format("{0}", ToEngineeringFormat.Convert(2100, 4, "Hz"));

            Prompt(result);
        }

        private static void Prompt(string message)
        {
            // Set the console color to green
            Console.ForegroundColor = ConsoleColor.Green;

            // Write the message and wait for a keypress
            Console.WriteLine("\n" + message + "\n");
            Console.ReadKey();

            // Reset the console color
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
