using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HP8350BTestApp
{
    /// <summary>
    /// Provides utility methods for formatting numbers in engineering notation with SI unit prefixes.
    /// </summary>
    /// <remarks>
    /// Engineering notation expresses numbers as a coefficient and a power of 1000, using standard SI prefixes
    /// (like m, µ, k, M, G, etc.). This is commonly used in engineering and scientific applications
    /// for expressing measurements with appropriate units. Implementation credit: Steve Hageman.
    /// http://analoghome.blogspot.com/2012/01/how-to-format-numbers-in-engineering.html
    /// </remarks>
    // All credit to Steve Hageman for the implementation
    // http://analoghome.blogspot.com/2012/01/how-to-format-numbers-in-engineering.html
    public static class ToEngineeringFormat
    {

        // Used in adding the units to the return string
        private static string[] prefix_const = { " y", " z", " a", " f", " p", " n", " u", " m", " ", " k", " M", " G", " T" };

        /// <summary>
        /// Converts a number to engineering notation with SI unit prefix.
        /// </summary>
        /// <param name="number">The number to convert.</param>
        /// <param name="significant_digits">
        /// The number of significant digits to return. Should be a minimum of 3 for engineering notation.
        /// Valid range is 1 to 15. When less than 3, output may be in scientific notation. Default is 3.
        /// </param>
        /// <param name="units">The measurement units (e.g., "Hz", "V", "A", "Ω"). Default is empty string.</param>
        /// <param name="fixedFormat">If true, uses fixed-point format instead of general format. Default is false.</param>
        /// <returns>A string representing the number in engineering notation with unit prefix and units.</returns>
        /// <remarks>
        /// Examples: 
        /// - Convert(0.001, 3, "V") returns "1.00 mV"
        /// - Convert(1000000, 3, "Hz") returns "1.00 MHz"
        /// - Convert(0.000000001, 3, "F") returns "1.00 nF"
        /// </remarks>
        // number: The number to convert.
        // 
        // significant_digits: The number of significant digits to return. digits should be a minimum of 3 for 
        // Engineering notation this routine will work with any number from 1 to 15
        // But when significant_digits is less than 3 the output may be in scientific notation
        //
        // units: what the number is a measure of, like "Hz", "Farads", "Tesla", etc.
        public static string Convert(double number, Int16 significant_digits = 3, string units = "", bool fixedFormat = false)
        {
            string format_str;

            double scale = Math.Log10(Math.Abs(number));
            if (scale < 0.0)
                scale += -3.0;

            // The + 0.001 here makes sure that we use the proper scale range by pushing the calculated range just a bit.
            Int16 power = (Int16)((scale / 3) + 0.001);
            string prefix_str = prefix_const[power + 8];
            double scale_factor = Math.Pow(10.0, (double)power * 3.0);
            double base_num = number / scale_factor;

            // Make the format specifier string - bound limit the digits first
            if (significant_digits < 1)
                significant_digits = 1;
            if (significant_digits > 15)
                significant_digits = 15;

            if (fixedFormat)
                format_str = "F" + significant_digits.ToString();
            else
                format_str = "G" + significant_digits.ToString();

            string converted_str = base_num.ToString(format_str) + prefix_str + units;

            return (converted_str);
        }
    }
}