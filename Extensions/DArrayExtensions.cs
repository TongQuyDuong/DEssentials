using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dessentials.Extensions
{
    public static class ArrayExtensions
    {
        [Serializable]
        private class ArrayWrapper<T>
        {
            public T[] array;
        }
        
        public static string ToJson<T>(T[] array)
        {
            var wrapper = new ArrayWrapper<T>();
            wrapper.array = array;
            string json = JsonUtility.ToJson(wrapper);
            json = json.Remove(0, 9);
            json = json.Remove(json.Length - 1);
            return json;
        }
    }
}
