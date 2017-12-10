using Ivi.Visa.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HPInstruments
{
    class InstrumentConnection
    {
        private string _address;

        private ResourceManager ResMgr = new ResourceManager();

        public FormattedIO488 Instrument
        {
            get
            {
                return Instrument;
            }

            private set
            {
                Instrument = new FormattedIO488();
            }
        }

        public bool TerminationCharacterEnabled
        {
            get
            {
                return TerminationCharacterEnabled;
            }
            set
            {
                Instrument.IO.TerminationCharacterEnabled = value;
            }
        }

        public int Timeout
        {
            get
            {
                return Timeout;
            }
            set
            {
                Instrument.IO.Timeout = value;
            }
        }
        
        public InstrumentConnection(string _address, AccessMode _mode = AccessMode.NO_LOCK, int _timeOut = 2000, string _option = null)
        {
            this._address = _address;

            Instrument.IO = (IMessage)ResMgr.Open(_address, _mode, _timeOut, _option);
        }

        public void Clear()
        {
            Instrument.IO.Clear();
        }
        
    }
}
