using System.Collections.Generic;

namespace Dessentials.Common.EntityManagement
{
    /// <summary>
    /// Generic static registry for any <see cref="IDisposableEntity"/> type.
    /// Use <c>Registry&lt;IDisposableEntity&gt;</c> for a global catch-all,
    /// or <c>Registry&lt;IMyInterface&gt;</c> for type-specific collections.
    /// Each closed generic gets its own independent list.
    /// </summary>
    public static class Registry<T> where T : IDisposableEntity
    {
        private static readonly List<T> s_entities = new();

        public static IReadOnlyList<T> All => s_entities;

        public static int Count => s_entities.Count;

        public static void Register(T entity)
        {
            s_entities.Add(entity);
        }

        public static void Unregister(T entity)
        {
            s_entities.Remove(entity);
        }

        public static void DisposeAll()
        {
            while (s_entities.Count > 0)
            {
                int lastIndex = s_entities.Count - 1;
                var entity = s_entities[lastIndex];
                entity.Dispose();

                // Safety: if Dispose() didn't unregister, remove manually
                if (lastIndex < s_entities.Count
                    && EqualityComparer<T>.Default.Equals(s_entities[lastIndex], entity))
                    s_entities.RemoveAt(lastIndex);
            }
        }

        public static void Clear()
        {
            s_entities.Clear();
        }
    }
}
