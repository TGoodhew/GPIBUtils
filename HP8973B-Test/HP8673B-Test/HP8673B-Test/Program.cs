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
            HP8673B.Device measuringReceiver = new Device(@"GPIB0::19::INSTR");
        }
    }
}
