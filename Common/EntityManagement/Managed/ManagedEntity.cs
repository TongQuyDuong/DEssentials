using UnityEngine;

namespace Dessentials.Common.EntityManagement
{
    public enum ManagedEntityState
    {
        InPool,
        InRegistry
    }

    public class ManagedEntity<TSelf> : MonoBehaviour
    where TSelf : ManagedEntity<TSelf>
    {
        public ManagedEntityState State { get; set; }

        public virtual void Dispose()
        {
            ManagedEntityFactory<TSelf>.Return((TSelf)this);
        }

        protected virtual void OnDestroy()
        {
            switch (State)
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
