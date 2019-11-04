using System;
using System.Collections.Generic;
using System.Text;
using NationalInstruments.Visa;
using Ivi.Visa;

namespace HP8340BOutputTest
{
    class HP8902A
    {
        private string address = @"GPIB0::14::INSTR";
        private GpibSession gpibSession;

        internal void Connect()
        {
            gpibSession = new GpibSession(address, AccessModes.None, 2000, true);

            gpibSession.TerminationCharacterEnabled = true;
            gpibSession.Clear();
        }
    }
}
