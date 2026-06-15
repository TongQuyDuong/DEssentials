using System.Collections.Generic;
using UnityEngine;

namespace Dessentials.Common.EntityManagement
{
    public static partial class EntityRegistry<T> where T : MonoBehaviour
    {
        private static readonly List<T> s_entities = new();

        public delegate bool SelectionFilter(T entity);

        public static IReadOnlyList<T> All
        {
            get
            {
                PurgeDestroyed();
                return s_entities;
            }
        }

        public static int Count
        {
            get
            {
                PurgeDestroyed();
                return s_entities.Count;
            }
        }

        public static void Register(T entity)
        {
            if (!s_entities.Contains(entity))
                s_entities.Add(entity);
        }

        public static void Unregister(T entity)
        {
            s_entities.Remove(entity);
        }

        public static T Query(SelectionFilter filter)
        {
            for (int i = s_entities.Count - 1; i >= 0; i--)
            {
                if (s_entities[i] == null)
                {
                    s_entities.RemoveAt(i);
                    continue;
                }

                if (filter(s_entities[i]))
                    return s_entities[i];
            }

            return null;
        }

        public static List<T> QueryAll(SelectionFilter filter)
        {
            var results = new List<T>();

            for (int i = s_entities.Count - 1; i >= 0; i--)
            {
                if (s_entities[i] == null)
                {
                    s_entities.RemoveAt(i);
                    continue;
                }

                if (filter(s_entities[i]))
                    results.Add(s_entities[i]);
            }

            return results;
        }

        public static int QueryNonAlloc(SelectionFilter filter, T[] buffer)
        {
            int count = 0;

            for (int i = s_entities.Count - 1; i >= 0; i--)
            {
                if (s_entities[i] == null)
                {
                    s_entities.RemoveAt(i);
                    continue;
                }

                if (filter(s_entities[i]))
                {
                    buffer[count++] = s_entities[i];
                    if (count >= buffer.Length)
                        break;
                }
            }

            return count;
        }

        public static void PurgeDestroyed()
        {
            for (int i = s_entities.Count - 1; i >= 0; i--)
                if (s_entities[i] == null)
                    s_entities.RemoveAt(i);
        }

        public static void Clear()
        {
            s_entities.Clear();
        }
    }
}
