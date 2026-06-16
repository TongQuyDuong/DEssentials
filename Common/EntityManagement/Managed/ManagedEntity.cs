using UnityEngine;

namespace Dessentials.Common.EntityManagement
{
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
    public class ManagedEntity<TSelf> : MonoBehaviour
    where TSelf : ManagedEntity<TSelf>
    {
        public ManagedEntityState ManagedEntityState { get; set; }

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
                    ManagedEntityRegistry<TSelf>.Unregister((TSelf)this);
                    break;
                case ManagedEntityState.InPool:
                    ManagedEntityFactory<TSelf>.PurgeDestroyed();
                    break;
            }
        }
    }
}
