using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Dessentials.Common.EntityManagement
{
    /// Counter bumped once per play session. Exists because Unity never invokes
    /// [RuntimeInitializeOnLoadMethod] on a generic type, so ManagedEntityFactory{T}
    /// cannot reset its own statics the way non-generic types do.
    internal static class ManagedEntityFactoryGeneration
    {
        internal static int Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Next() => Current++;
    }

    public static class ManagedEntityFactory<TObject> where TObject : ManagedEntity<TObject>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static GameObject s_prefab;
        // ReSharper disable once StaticMemberInGenericType
        private static AsyncOperationHandle<GameObject> s_prefabHandle;
        private static readonly Stack<TObject> s_pool = new();
        // ReSharper disable once StaticMemberInGenericType
        private static int s_generation = -1;

        private static string AddressableID => typeof(TObject).Name;

        /// Drops state left over from a previous play session. Only does anything when
        /// Enter Play Mode Options has Reload Domain switched off. s_prefabHandle is the
        /// reason this is needed: it is a struct, so unlike the prefab GameObject and the
        /// pooled components it never becomes Unity's fake-null and would otherwise be
        /// released against a refcount from the previous session.
        private static void EnsureCurrentGeneration()
        {
            if (s_generation == ManagedEntityFactoryGeneration.Current)
                return;

            s_generation = ManagedEntityFactoryGeneration.Current;
            s_prefab = null;
            s_prefabHandle = default;
            s_pool.Clear();
        }

        private static async UniTask<GameObject> LoadPrefabAsync()
        {
            if (s_prefab != null)
                return s_prefab;

            s_prefabHandle = Addressables.LoadAssetAsync<GameObject>(AddressableID);
            s_prefab = await s_prefabHandle.ToUniTask();
            return s_prefab;
        }

        public static async UniTask<TObject> GetAsync(Transform parent = null)
        {
            EnsureCurrentGeneration();

            TObject instance;

            if (s_pool.Count > 0)
            {
                instance = s_pool.Pop();
                instance.transform.SetParent(parent);
                instance.gameObject.SetActive(true);
            }
            else
            {
                var prefab = await LoadPrefabAsync();
                instance = Object.Instantiate(prefab, parent).GetComponent<TObject>();
            }

            instance.ManagedEntityState = ManagedEntityState.InRegistry;
            ManagedEntityRegistry<TObject>.Register(instance);
            Registry<IDisposableEntity>.Register(instance);
            return instance;
        }

        public static void Return(TObject obj)
        {
            ManagedEntityRegistry<TObject>.Unregister(obj);
            Registry<IDisposableEntity>.Unregister(obj);
            obj.ManagedEntityState = ManagedEntityState.InPool;
            obj.gameObject.SetActive(false);
            s_pool.Push(obj);
        }

        public static bool TryReturn(GameObject go)
        {
            if (go == null) return false;
            if (!go.TryGetComponent<TObject>(out var component)) return false;

            Return(component);
            return true;
        }

        public static void Preload(int count, Transform parent = null)
        {
            PreloadAsync(count, parent).Forget();
        }

        public static async UniTask PreloadAsync(int count, Transform parent = null)
        {
            EnsureCurrentGeneration();

            var prefab = await LoadPrefabAsync();

            for (int i = 0; i < count; i++)
            {
                var instance = Object.Instantiate(prefab, parent).GetComponent<TObject>();
                instance.ManagedEntityState = ManagedEntityState.InPool;
                instance.gameObject.SetActive(false);
                s_pool.Push(instance);
            }
        }

        public static void PurgeDestroyed()
        {
            var count = s_pool.Count;
            if (count == 0) return;

            var temp = System.Buffers.ArrayPool<TObject>.Shared.Rent(count);
            int kept = 0;

            while (s_pool.Count > 0)
            {
                var obj = s_pool.Pop();
                if (obj != null)
                    temp[kept++] = obj;
            }

            for (int i = kept - 1; i >= 0; i--)
                s_pool.Push(temp[i]);

            System.Buffers.ArrayPool<TObject>.Shared.Return(temp, true);
        }

        public static void Dispose()
        {
            EnsureCurrentGeneration();

            while (s_pool.Count > 0)
            {
                var obj = s_pool.Pop();
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }

            if (s_prefabHandle.IsValid())
                Addressables.Release(s_prefabHandle);

            s_prefab = null;
        }
    }
}
