using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if DESSENTIALS_VCONTAINER
using VContainer;
using VContainer.Unity;
#endif

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

    /// Cách entity mới Instantiate được dựng. Mặc định là Object.Instantiate trần.
    ///
    /// Bật DESSENTIALS_VCONTAINER thì thêm `Resolver`: gắn IObjectResolver vào đó
    /// (LifetimeScope gắn lúc Awake, gỡ lúc OnDestroy) là entity spawn ra được inject.
    /// Bỏ trống thì vẫn rơi về Object.Instantiate, nên factory dùng được cả ngoài scope.
    // ponytail: một resolver global, đủ cho một LifetimeScope sống tại một thời điểm.
    // Có hai scope chồng nhau thì đổi sang truyền resolver vào GetAsync/PreloadAsync.
    public static class ManagedEntityInjection
    {
#if DESSENTIALS_VCONTAINER
        public static IObjectResolver Resolver { get; set; }
#endif

        internal static T Instantiate<T>(GameObject prefab, Transform parent) where T : Component
        {
#if DESSENTIALS_VCONTAINER
            var instance = Resolver != null
                ? Resolver.Instantiate(prefab, parent)
                : Object.Instantiate(prefab, parent);
#else
            var instance = Object.Instantiate(prefab, parent);
#endif
            return instance.GetComponent<T>();
        }
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

        /// Chỉ đặt LoadAssetAsync một lần: Preload chạy song song với Get (hoặc hai Get sát nhau)
        /// mà đặt hai lần thì lần sau ghi đè s_prefabHandle và bỏ rơi refcount của lần đầu.
        /// Người đến sau await lại chính handle đó — Addressables cho nhiều người cùng nghe Completed,
        /// còn UniTask.Preserve() thì không: nó chỉ nhớ kết quả, awaiter thứ hai lúc còn pending
        /// sẽ ăn "Already continuation registered".
        private static async UniTask<GameObject> LoadPrefabAsync()
        {
            if (s_prefab != null)
                return s_prefab;

            if (!s_prefabHandle.IsValid())
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
                instance.transform.SetParent(parent, false);
                instance.RestoreSpawnTransform();
                instance.gameObject.SetActive(true);
            }
            else
            {
                var prefab = await LoadPrefabAsync();
                instance = ManagedEntityInjection.Instantiate<TObject>(prefab, parent);
                instance.CaptureSpawnTransform();
            }

            instance.ManagedEntityState = ManagedEntityState.InRegistry;
            ManagedEntityRegistry<TObject>.Register(instance);
            Registry<IDisposableEntity>.Register(instance);
            return instance;
        }

        public static void Return(TObject obj)
        {
            // Cũng phải kiểm generation như Get: Return đến trước Get đầu tiên của session mới
            // thì obj sẽ bị dọn cùng pool cũ ngay ở lần Get sau đó.
            EnsureCurrentGeneration();

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

            // Bơm cho pool đủ count chứ không cộng thêm count: Preload gọi lại ở mỗi màn thì
            // pool sẽ phình mãi. Đọc s_pool.Count sau await vì trong lúc chờ pool có thể đã đầy.
            while (s_pool.Count < count)
            {
                var instance = ManagedEntityInjection.Instantiate<TObject>(prefab, parent);
                instance.CaptureSpawnTransform();
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
            s_prefabHandle = default;
        }
    }
}
