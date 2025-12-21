using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS1054Z
{
    public class DataPoint
    {
        public int X { get; set; }
        public byte Y { get; set; }
    }

    public class ChartViewModel
    {
        public List<DataPoint> ByteSeries { get; }

        public ChartViewModel(byte[] data)
        {
            ByteSeries = ConvertBytes(data);
        }

        public List<DataPoint> ConvertBytes(byte[] bytes)
        {
            var list = new List<DataPoint>();

            for (int i = 0; i < bytes.Length; i++)
            {
                list.Add(new DataPoint { X = i, Y = bytes[i] });
            }

            return list;
        }
    }
}
