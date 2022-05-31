using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HP8350BTestApp
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