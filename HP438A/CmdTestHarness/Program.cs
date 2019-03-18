using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ivi.Visa;

namespace CmdTestHarness
{
    class Program
    {
        public static bool BPause { get; set; }

        private static SemaphoreSlim signal = new SemaphoreSlim(0, 1);        

        static void Main(string[] args)
        {
            

            string PWRMeterAddress = @"GPIB0::9::INSTR";

            using (IGpibSession pwrMeter = (IGpibSession)GlobalResourceManager.Open("GPIB0::9::INSTR") as IGpibSession)
            {
                BPause = true;

                pwrMeter.Clear();
                pwrMeter.ServiceRequest += PwrMeter_ServiceRequest;

                pwrMeter.FormattedIO.WriteLine("@1\u0002");
                pwrMeter.FormattedIO.WriteLine("ZE");

                signal.Wait();

                pwrMeter.FormattedIO.WriteLine("?ID");
                var id = pwrMeter.FormattedIO.ReadLine();


            }

        }

        private static void PwrMeter_ServiceRequest(object sender, VisaEventArgs e)
        {
            BPause = false;

            signal.Release();
        }
    }
}
