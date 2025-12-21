using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS1054Z
{
    /// <summary>
    /// Provides utilities for formatting numeric values in engineering notation with metric prefixes.
    /// </summary>
    /// <remarks>
    /// All credit to Steve Hageman for the implementation:
    /// http://analoghome.blogspot.com/2012/01/how-to-format-numbers-in-engineering.html
    /// </remarks>
    public static class ToEngineeringFormat
    {

        // Metric prefixes for engineering notation (10^-24 to 10^12)
        // Index 8 represents no prefix (10^0)
        private static string[] prefix_const = { " y", " z", " a", " f", " p", " n", " u", " m", " ", " k", " M", " G", " T" };

        /// <summary>
        /// Converts a number to engineering notation with metric prefixes.
        /// </summary>
        /// <param name="number">The number to convert.</param>
        /// <param name="significant_digits">
        /// The number of significant digits to return. Should be a minimum of 3 for proper engineering notation.
        /// This method works with any number from 1 to 15, but when less than 3, the output may be in scientific notation.
        /// </param>
        /// <param name="units">The unit of measurement (e.g., "Hz", "V", "A", "F").</param>
        /// <param name="fixedFormat">If true, uses fixed-point format instead of general format.</param>
        /// <returns>A string representation of the number in engineering notation with appropriate metric prefix.</returns>
        /// <remarks>
        /// For numbers with a magnitude that doesn't fit within the available metric prefixes,
        /// the method falls back to scientific (exponential) notation.
        /// </remarks>
        public static string Convert(double number, Int16 significant_digits = 3, string units = "", bool fixedFormat = false)
        {
            // Guard special numeric values
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                // Preserve the representation for NaN/Infinity
                return number.ToString() + units;
            }

            // Normalize significant digits within allowed range (1-15)
            int sd = significant_digits;
            if (sd < 1) sd = 1;
            if (sd > 15) sd = 15;

            string format_str = (fixedFormat ? "F" : "G") + sd.ToString();

            // Zero is a common case; return immediately using the 'no prefix' entry (index 8)
            if (number == 0.0)
            {
                return (0.0).ToString(format_str) + prefix_const[8] + units;
            }

            // Calculate the scale (power of 10) of the number
            double scale = Math.Log10(Math.Abs(number));
            if (scale < 0.0)
                scale += -3.0;

            // The + 0.001 adjustment ensures proper scale range selection
            Int16 power = (Int16)((scale / 3) + 0.001);
            int index = power + 8; // Offset by 8 to align with prefix_const array

            // If the computed prefix index is out of range, fallback to scientific (exponential) format
            if (index < 0 || index >= prefix_const.Length)
            {
                string sciFmt = "E" + sd.ToString();
                return number.ToString(sciFmt) + units;
            }

            string prefix_str = prefix_const[index];
            
            // Scale the number to the appropriate range (e.g., 1000 Hz becomes 1 kHz)
            double scale_factor = Math.Pow(10.0, (double)power * 3.0);
            double base_num = number / scale_factor;

            string converted_str = base_num.ToString(format_str) + prefix_str + units;

            return converted_str;
        }
    }
}