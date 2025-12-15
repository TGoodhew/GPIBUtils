using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS1054Z
{
    // All credit to Steve Hageman for the implementation
    // http://analoghome.blogspot.com/2012/01/how-to-format-numbers-in-engineering.html
    public static class ToEngineeringFormat
    {

        // Used in adding the units to the return string
        private static string[] prefix_const = { " y", " z", " a", " f", " p", " n", " u", " m", " ", " k", " M", " G", " T" };

        // number: The number to convert.
        // 
        // significant_digits: The number of significant digits to return. digits should be a minimum of 3 for 
        // Engineering notation this routine will work with any number from 1 to 15
        // But when significant_digits is less than 3 the output may be in scientific notation
        //
        // units: what the number is a measure of, like "Hz", "Farads", "Tesla", etc.
        public static string Convert(double number, Int16 significant_digits = 3, string units = "", bool fixedFormat = false)
        {
            // Guard special numeric values
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                // Preserve the representation for NaN/Infinity
                return number.ToString() + units;
            }

            // Normalize significant digits within allowed range
            int sd = significant_digits;
            if (sd < 1) sd = 1;
            if (sd > 15) sd = 15;

            string format_str = (fixedFormat ? "F" : "G") + sd.ToString();

            // Zero is a common case; return immediately using the 'no prefix' entry (index 8)
            if (number == 0.0)
            {
                return (0.0).ToString(format_str) + prefix_const[8] + units;
            }

            double scale = Math.Log10(Math.Abs(number));
            if (scale < 0.0)
                scale += -3.0;

            // The + 0.001 here makes sure that we use the proper scale range by pushing the calculated range just a bit.
            Int16 power = (Int16)((scale / 3) + 0.001);
            int index = power + 8;

            // If the computed prefix index is out of range, fallback to scientific (exponential) format
            if (index < 0 || index >= prefix_const.Length)
            {
                string sciFmt = "E" + sd.ToString();
                return number.ToString(sciFmt) + units;
            }

            string prefix_str = prefix_const[index];
            double scale_factor = Math.Pow(10.0, (double)power * 3.0);
            double base_num = number / scale_factor;

            string converted_str = base_num.ToString(format_str) + prefix_str + units;

            return converted_str;
        }
    }
}