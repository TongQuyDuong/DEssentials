using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Dessentials.Extensions
{
    public static class DMathExtensions
    {
        /// <summary>
        /// Formats an integer into a string with thousands separated by a space, only 10k and above will get formatted
        /// </summary>
        /// <param name="number">The integer to format.</param>
        /// <returns>A string representation of the number with space separators.</returns>
        public static string FormatIntWithSpaceSeparator(this int number)
        {
            if (number < 10000)
                return number.ToString();
            
            NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            nfi.NumberGroupSeparator = " ";
            return number.ToString("#,0", nfi);
        }
    }
}
