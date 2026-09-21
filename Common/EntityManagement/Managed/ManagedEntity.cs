using UnityEngine;

namespace Dessentials.Common.EntityManagement
{
    internal static class UnityObjectAlive
    {
        /// <summary>
        /// Kiểm tra "còn sống" cho code generic. Với type parameter thì <c>x == null</c> chỉ là so
        /// sánh tham chiếu: C# không lấy operator == của UnityEngine.Object cho type parameter, nên
        /// object đã bị destroy sẽ lọt qua như còn sống. Ép về Object trước rồi mới so mới đúng.
        /// </summary>
        internal static bool IsAlive(Object obj) => obj != null;
    }

    /// Lifecycle state of a managed entity.
    /// The factory sets this automatically on Get (InRegistry) and Return (InPool).
    public enum ManagedEntityState
    {
        InPool,
        InRegistry
    }

    /// Base class for entities whose lifecycle is managed by ManagedEntityFactory and ManagedEntityRegistry.
    ///
    /// The system works as follows:
    ///   - ManagedEntityFactory loads a prefab via Addressables using the type name as the key,
    ///     pools instances for reuse, and tracks active ones in ManagedEntityRegistry.
    ///   - ManagedEntityRegistry is a static queryable collection of all active entities of a given type.
    ///   - Call Dispose() to return an entity to the pool. Override it to add cleanup logic.
    ///   - If a managed entity is destroyed without being returned (e.g. scene unload),
    ///     OnDestroy auto-cleans: unregisters if active, or purges the pool if pooled.
    public class ManagedEntity<TSelf> : MonoBehaviour, IDisposableEntity
    where TSelf : ManagedEntity<TSelf>
    {
        public ManagedEntityState ManagedEntityState { get; set; }

        // TRS gốc của prefab. Factory chụp lại ngay sau Instantiate(prefab, parent,
        // worldPositionStays: false), lúc local values còn đúng bằng của prefab — không đọc ở Awake
        // vì Awake không chạy trong Edit Mode, cũng không hỏi lại prefab asset vì factory
        // không phải giữ nó sống chỉ để biết mấy con số này.
        private Vector3 spawnLocalPosition;
        private Quaternion spawnLocalRotation = Quaternion.identity;
        private Vector3 spawnLocalScale = Vector3.one;

        /// Của factory gọi, không gọi tay: chụp TRS ngay sau khi Instantiate.
        public void CaptureSpawnTransform()
        {
            var t = transform;
            spawnLocalPosition = t.localPosition;
            spawnLocalRotation = t.localRotation;
            spawnLocalScale = t.localScale;
        }

        /// Trả transform về đúng như lúc mới Instantiate. Factory gọi mỗi lần lấy ra khỏi pool,
        /// vì instance còn mang tư thế của lần dùng trước và của parent cũ.
        public void RestoreSpawnTransform()
        {
            var t = transform;
            t.localPosition = spawnLocalPosition;
            t.localRotation = spawnLocalRotation;
            t.localScale = spawnLocalScale;
        }

        /// Returns this entity to the factory pool. Override to add cleanup before pooling.
        public virtual void Dispose()
        {
            ManagedEntityFactory<TSelf>.Return((TSelf)this);
        }

        /// Safety net: cleans up stale references when Unity destroys this object unexpectedly.
        protected virtual void OnDestroy()
        {
            switch (ManagedEntityState)
            {
                case ManagedEntityState.InRegistry:
                    // Gỡ khỏi cả hai chỗ Get đã đăng ký, y như Return làm — bỏ sót Registry thì
                    // nó tích xác qua từng lần load scene.
                    ManagedEntityRegistry<TSelf>.Unregister((TSelf)this);
                    Registry<IDisposableEntity>.Unregister(this);
                    break;
                case ManagedEntityState.InPool:
                    ManagedEntityFactory<TSelf>.PurgeDestroyed();
                    break;
            }
        }
    }
}
