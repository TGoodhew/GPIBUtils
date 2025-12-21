using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS1054Z
{
    /// <summary>
    /// Represents a single data point in a chart with an X position and Y value.
    /// </summary>
    public class DataPoint
    {
        /// <summary>
        /// Gets or sets the X-axis position (sample index) of this data point.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the Y-axis value (sample amplitude) of this data point.
        /// </summary>
        public byte Y { get; set; }
    }

    /// <summary>
    /// View model for chart data that converts byte arrays into a collection of data points
    /// suitable for display in a charting component.
    /// </summary>
    public class ChartViewModel
    {
        /// <summary>
        /// Gets the collection of data points converted from the byte array.
        /// </summary>
        public List<DataPoint> ByteSeries { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChartViewModel"/> class.
        /// </summary>
        /// <param name="data">The byte array to convert into data points.</param>
        public ChartViewModel(byte[] data)
        {
            ByteSeries = ConvertBytes(data);
        }

        /// <summary>
        /// Converts a byte array into a list of data points where each byte becomes
        /// a Y value and its array index becomes the X value.
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <returns>A list of data points with sequential X values and byte Y values.</returns>
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
