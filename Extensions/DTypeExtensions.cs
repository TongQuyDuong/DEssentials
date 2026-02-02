using System;
using System.Collections;
using System.Collections.Generic;

namespace Dessentials.Extensions
{
    public static class DTypeExtensions
    {
        public static T[] GetAllEnumValues<T>()
        where T : struct, Enum
        {
            return (T[])Enum.GetValues(typeof(T));
        }
    }
}
