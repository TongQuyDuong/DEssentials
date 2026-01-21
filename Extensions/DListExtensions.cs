using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dessentials.Extensions
{
    public static class ListExtensions
    {
        public static T GetRandomFromList<T>(this List<T> list)
        {
            if (list.Count == 0) return default(T);
            
            UnityEngine.Random.InitState(Guid.NewGuid().GetHashCode());

            T random = list[UnityEngine.Random.Range(0, list.Count)];
            return random;
        }
        public static List<T> GetRandomElements<T>(this IEnumerable<T> list, int elementsCount)
        {
            return list.OrderBy(arg => Guid.NewGuid()).Take(elementsCount).ToList();
        }
        
        public static List<T> GetRandomElementsInGroups<T, TKeySelector>(this IEnumerable<T> list, int elementsCount, Func<T, TKeySelector> keySelector)
        {
            var iEnumerable = list.ToList();
            if (elementsCount >= iEnumerable.Count())
                return iEnumerable.ToList();
            
            var resultList = new List<T>();
            var remainingCount = elementsCount;
            
            var groups = iEnumerable.GroupBy(keySelector).OrderBy(arg => Guid.NewGuid()).ToList();
            
            foreach (var group in groups)
            {
                if (remainingCount <= 0)
                    break;
                
                var takeCount = Math.Min(remainingCount, group.Count());
                resultList.AddRange(group.Take(takeCount));
                remainingCount -= takeCount;
            }
            
            return resultList;
        }

        public static T GetRandomFromListWithProrityPredicates<T>(this List<T> list, params Predicate<T>[] predicates)
        {
            if (list.Count == 0) return default(T);
            if (predicates == null || predicates.Length == 0) return list.GetRandomFromList();

            for (int i = 0; i < predicates.Length; i++)
            {
                var predicateList = predicates.TakeLast(predicates.Length - i);
                
                var tempList = list.Where(item => predicateList.All(predicate => predicate(item))).ToList();
                if (tempList.Count > 0)
                {
                    return tempList.GetRandomFromList();
                }
            }
            
            return list.GetRandomFromList();
        }
        
        public static List<T> GetEvenlyDistributedRandomElements<T>(this List<T> list, int elementsCount)
        {
            var sourceList = list.ToList();
            if (elementsCount >= sourceList.Count)
                return sourceList;
            
            if (elementsCount <= 0)
                return new List<T>();
            
            var result = new List<T>();
            var rng = new System.Random();
            var usedIndices = new HashSet<int>();
            
            // calculate bin size
            float binSize = (float)sourceList.Count / elementsCount;
            
            // get 1 random element from each bin
            for (int i = 0; i < elementsCount; i++)
            {
                int binStart = (int)(i * binSize);
                int binEnd = (int)((i + 1) * binSize);
                
                // Đảm bảo bin cuối cùng bao gồm tất cả phần tử còn lại
                if (i == elementsCount - 1)
                    binEnd = sourceList.Count;
                
                // Chọn ngẫu nhiên một index trong bin, đảm bảo không trùng
                int randomIndex;
                int attempts = 0;
                do
                {
                    randomIndex = rng.Next(binStart, binEnd);
                    attempts++;
                    
                    // Nếu thử quá nhiều lần, mở rộng phạm vi tìm kiếm
                    if (attempts > (binEnd - binStart) * 2)
                    {
                        // Tìm index chưa dùng gần nhất trong bin
                        for (int j = binStart; j < binEnd; j++)
                        {
                            if (!usedIndices.Contains(j))
                            {
                                randomIndex = j;
                                break;
                            }
                        }
                        break;
                    }
                } while (usedIndices.Contains(randomIndex));
                
                usedIndices.Add(randomIndex);
                result.Add(sourceList[randomIndex]);
            }
            
            return result;
        }
        
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
        
        public static List<List<T>> DivideEqually<T>(this List<T> list, int numberOfSubLists)
        {
            if (list == null || list.Count == 0)
            {
                Debug.LogWarning("Input list is empty. Nothing to divide.");
                return new List<List<T>>();
            }

            if (numberOfSubLists <= 0)
            {
                Debug.LogError("Number of sub-lists must be greater than 0.");
                return new List<List<T>>();
            }

            // Clear previous results
            var result = new List<List<T>>(numberOfSubLists);

            // Initialize the sub-list wrappers
            for (int i = 0; i < numberOfSubLists; i++)
            {
                result.Add(new List<T>());
            }

            // --- Alternative Logic (Fill one sub-list, then the next) ---
            int itemsPerSubListBase = list.Count / numberOfSubLists;
            int remainderItems = list.Count % numberOfSubLists;
            int currentInputIndex = 0;

            for (int i = 0; i < numberOfSubLists; i++) // Iterate through each sub-list to populate
            {
                // Determine how many items this sub-list gets
                int itemsInThisSubList = itemsPerSubListBase;
                if (remainderItems > 0)
                {
                    itemsInThisSubList++;
                    remainderItems--; // Decrement remainder for next lists
                }

                // Populate the current sub-list (dividedLists[i])
                for (int j = 0; j < itemsInThisSubList; j++)
                {
                    if (currentInputIndex < list.Count) // Ensure we don't go out of bounds of inputList
                    {
                        // Optionally skip null entries from the input list, or include them
                        // if (inputList[currentInputIndex] != null) // If you want to skip nulls
                        // {
                        result[i].Add(list[currentInputIndex]);
                        // }
                        currentInputIndex++;
                    }
                    else
                    {
                        break; // No more items in input list to distribute
                    }
                }
            }

            Debug.Log($"Input list of {list.Count} items divided into {numberOfSubLists} sub-lists using sequential fill logic.");

            return result;
        }
        
        public static void DSwap<T>(this IList<T> list, int i, int j)
        {
            (list[i], list[j]) = (list[j], list[i]);
        }
        
        public static void DShuffle<T>(this IList<T> list, int seed)
        {
            UnityEngine.Random.InitState(seed);
            for (int i = 0; i < list.Count; i++)
            {
                list.DSwap(i, UnityEngine.Random.Range(i, list.Count));
            }
        }

        public static void DShuffle<T>(this IList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list.DSwap(i, UnityEngine.Random.Range(i, list.Count));
            }
        }
        
        public static string ToJson<T>(List<T> list)
        {
            var wrapper = new ListWrapper<T>();
            wrapper.list = list;
            string json = JsonUtility.ToJson(wrapper);
            json = json.Remove(0, 8);
            json = json.Remove(json.Length - 1);
            return json;
        }
        
        
        [Serializable]
        private class ListWrapper<T>
        {
            public List<T> list;
        }
    }
}
