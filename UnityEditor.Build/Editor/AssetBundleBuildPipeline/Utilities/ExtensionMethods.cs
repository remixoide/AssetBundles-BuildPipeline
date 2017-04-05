using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEditor.Build.Utilities
{
    public static class ExtensionMethods
    {
        public static bool IsNullOrEmpty<T> (this ICollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }

        public static void Swap<T>(this IList<T> array, int index1, int index2)
        {
            var t = array[index2];
            array[index2] = array[index1];
            array[index1] = t;
        }

        public static void Swap<T>(this T[] array, int index1, int index2)
        {
            var t = array[index2];
            array[index2] = array[index1];
            array[index1] = t;
        }
    }
}
