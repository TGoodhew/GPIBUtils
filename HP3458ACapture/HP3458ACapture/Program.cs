using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Ivi.Visa;
using NationalInstruments.Visa;

namespace HP3458ACapture
{
    class Program
    {
        private static string address = @"GPIB0::22::INSTR";
        private static GpibSession session;
        private static IMessageBasedFormattedIO io;
        private static bool running = false;

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                System.Console.WriteLine(@"Usage is: HP3458ACapture {Range name} {Minutes}");
                return;
            }

            // Setup the session and get the IO interface
            session = new GpibSession(address);
            io = session.FormattedIO;

            // Setup the 3458A for the AC reading
            Setup3458A();

            var filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\acvolts-" + args[0] + ".csv";

            // Open the file
            using (StreamWriter csvFile = new StreamWriter(filePath))
            {
                var runtime = 60 * 1000 * Int32.Parse(args[1]);
                var timer = new Timer(runtime);
                timer.Elapsed += Timer_Elapsed;
                running = true;
                timer.Start();

                var newLine = "Time,Voltage (" + args[0] + " range)";
                Console.WriteLine(newLine);
                csvFile.WriteLine(newLine);

                while (running)
                {
                    newLine = $"{DateTime.Now.ToString("HH:mm:ss.ffffff")},{io.ReadString()}";
                    Console.WriteLine(newLine);
                    csvFile.WriteLine(newLine);
                }
            }

            io.WriteLine("RESET");
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            running = false;
        }

        private static void Session_ServiceRequest(object sender, VisaEventArgs e)
        {
            StatusByteFlags flags  = session.ReadStatusByte();

            Console.WriteLine(flags.ToString());

            if (flags.HasFlag(StatusByteFlags.User7))
                Console.WriteLine("{0} - {1}", DateTime.Now.ToString("HH:mm:ss.ffffff"), io.ReadLine());
        }

        private static void Setup3458A()
        {
            var setupCMDs = new string[] { "RESET", "END ALWAYS", "TARM HOLD", "SETACV SYNC", "RES .001", "FUNC 2", "TARM AUTO" };

            foreach (var cmd in setupCMDs)
            {
                io.WriteLine(cmd);
            }
        }
    }
}
