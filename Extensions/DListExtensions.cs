using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dessentials.Extensions
{
    public static class ListExtensions
    {
        public static int RemoveNullElements<T>(this List<T> list, bool logResults = true) where T : UnityEngine.Object
        {
            if (list == null)
            {
                if (logResults)
                    Debug.LogWarning("Cannot clean a null list.");
                return 0;
            }

            int initialCount = list.Count;

            // Create a new list with only valid (non-null) elements
            var validElements = list.Where(item => item != null).ToList();

            int removedCount = initialCount - validElements.Count;

            // Clear the original list and add back only the valid elements
            list.Clear();
            list.AddRange(validElements);

            if (logResults && removedCount > 0)
            {
                Debug.Log($"Removed {removedCount} null/missing element{(removedCount != 1 ? "s" : "")} from list. " +
                          $"List now contains {list.Count} element{(list.Count != 1 ? "s" : "")}.");
            }

            return removedCount;
        }

        public static TSource MinBy<TSource>(this List<TSource> source, Func<TSource, float> keySelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            if (source.Count == 0)
            {
                return default(TSource);
            }

            TSource minElement = source[0];
            float minKey = keySelector(minElement);

            for (int i = 1; i < source.Count; i++)
            {
                TSource currentElement = source[i];
                float currentKey = keySelector(currentElement);

                if (float.IsNaN(currentKey) || float.IsNaN(minKey))
                {
                    if (float.IsNaN(minKey) && !float.IsNaN(currentKey))
                    {
                        minKey = currentKey;
                        minElement = currentElement;
                    }

                    continue;
                }

                if (currentKey < minKey)
                {
                    minKey = currentKey;
                    minElement = currentElement;
                }
            }
            return minElement;
        }

        public static TSource MaxBy<TSource>(this List<TSource> source, Func<TSource, float> keySelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            if (source.Count == 0)
            {
                return default(TSource);
            }

            TSource maxElement = source[0];
            float maxKey = keySelector(maxElement);

            for (int i = 1; i < source.Count; i++)
            {
                TSource currentElement = source[i];
                float currentKey = keySelector(currentElement);

                if (float.IsNaN(currentKey) || float.IsNaN(maxKey))
                {
                    if (float.IsNaN(maxKey) && !float.IsNaN(currentKey))
                    {
                        maxKey = currentKey;
                        maxElement = currentElement;
                    }
                    continue;
                }

                if (currentKey > maxKey)
                {
                    maxKey = currentKey;
                    maxElement = currentElement;
                }
            }
            return maxElement;
        }
    }
}
