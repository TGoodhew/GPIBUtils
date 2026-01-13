using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAppCommon
{
    /// <summary>
    /// Provides utility methods for formatted console output with color-coded messages.
    /// </summary>
    /// <remarks>
    /// This class helps create consistent, user-friendly console output for test applications.
    /// It supports different message types (prompts, headings, information) with appropriate coloring
    /// and provides automatic console buffer configuration to prevent log truncation.
    /// </remarks>
    public static class Output
    {
        /// <summary>
        /// Configures the console for optimal output display.
        /// </summary>
        /// <param name="bufferHeight">
        /// The desired console buffer height in lines. Default is 9999 to allow extensive logging.
        /// Valid range is implementation-dependent. Invalid values fall back to system default.
        /// </param>
        /// <remarks>
        /// This method sets the console foreground color to white and attempts to set the buffer height.
        /// A larger buffer height prevents log truncation during long test runs. If the requested
        /// buffer height is invalid, the method silently uses the system default.
        /// </remarks>
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
                // If the requested buffer height is invalid, silently use system default
                // This can happen if bufferHeight is too small or exceeds system limits
                // Failing silently is acceptable as buffer height is not critical to app functionality
            }
        }
        /// <summary>
        /// Displays a prompt message in green, beeps, and waits for user input.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <remarks>
        /// This method is typically used to pause execution and wait for user acknowledgment.
        /// The message is displayed in green with a beep to draw attention. After the user presses
        /// any key, the console color is reset to white and execution continues.
        /// </remarks>
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

        /// <summary>
        /// Displays a heading message in blue, optionally with a beep.
        /// </summary>
        /// <param name="message">The heading text to display.</param>
        /// <param name="beep">If true, plays a beep sound. Default is false.</param>
        /// <remarks>
        /// Headings are used to separate major sections or steps in test output.
        /// The message is displayed in blue to distinguish it from other output types.
        /// After display, the console color is reset to white.
        /// </remarks>
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

        /// <summary>
        /// Displays an informational message in white (the default console color).
        /// </summary>
        /// <param name="message">The informational message to display.</param>
        /// <remarks>
        /// This method is used for general informational output, test results, or data display.
        /// The message is displayed in white, which is the base console color, so no color reset is needed.
        /// </remarks>
        public static void Information(string message)
        {
            // Set the console color to white (already base color so no need to reset)
            Console.ForegroundColor = ConsoleColor.White;

            // Write the message
            Console.WriteLine(message + "\n");
        }
    }
}
