using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Ivi.Visa.Interop;

namespace FrequencyCounter
{
    enum Mode { DCV, ACV, DCI, ACI, OHM };

    public class HP5342A : DependencyObject
    {
        private Mode CurrentMode;
        private string CurrentCommand;
        private string DMMAddress = @"TCPIP0::192.168.1.25::inst0::INSTR";
        private ResourceManager ResMgr = new ResourceManager();
        private FormattedIO488 DMM = new FormattedIO488();
        

        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            protected set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(HP5342A));

        public HP5342A(string VISAAddress)
        {
            DMMAddress = VISAAddress;
        }

        public object Open()
        {
            DMM.IO = (IMessage)ResMgr.Open(DMMAddress, AccessMode.NO_LOCK, 2000, null);
            DMM.IO.TerminationCharacterEnabled = true;
            DMM.IO.Timeout = 20000;
            DMM.IO.Clear();

            return true;
        }
    }
}
