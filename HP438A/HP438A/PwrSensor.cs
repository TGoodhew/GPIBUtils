using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HP438A
{
    public class PwrSensor
    {
        // Sensor name like HP8481A
        public string Name;

        // Sensor serial number like 1234A12345
        public string SerialNumber;


        // Sorted dictionary that contains the calibration factor table for the sensor
        // in the format (frequency, calibration factor)
        public SortedDictionary<long, double> CalFactorTable = new SortedDictionary<long, double>();

        // Get (and interpolate if required) the calbration factor for a given frequency
        public double GetCalFactorForFrequency(long freq)
        {
            // Get the dictionary pair that is on either side of the requested freq
            var freqPair = CalFactorTable.Keys.Zip(CalFactorTable.Keys.Skip(1),
                (a, b) => new { a, b })
                .Where(x => x.a <= freq && x.b >= freq)
                .FirstOrDefault();

            // Interpolate as a HP437A does
            // Y2 = Y0(X2-X1)/(X0-X1)+Y1(X2-X0)/(X1-X0)

            var result = CalFactorTable[freqPair.a] * (freq - freqPair.b) / (freqPair.a - freqPair.b) + 
                            CalFactorTable[freqPair.b] * (freq - freqPair.a) / (freqPair.b - freqPair.a);

            return result;
        }
    }
}
