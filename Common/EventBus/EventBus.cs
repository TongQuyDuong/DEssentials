using System.Collections.Generic;
using UnityEngine;

namespace Dessentials.Common.EventBus
{
    public static class EventBus<T> where T : IEvent
    {
        static readonly List<IEventBinding<T>> bindings = new();
        static bool isRaising;
        static readonly List<IEventBinding<T>> pendingAdds = new();
        static readonly List<IEventBinding<T>> pendingRemoves = new();

        public static void Register(EventBinding<T> binding)
        {
            if (isRaising)
            {
                pendingAdds.Add(binding);
                return;
            }

            bindings.Add(binding);
        }

        public static void Deregister(EventBinding<T> binding)
        {
            if (isRaising)
            {
                pendingRemoves.Add(binding);
                return;
            }

            bindings.Remove(binding);
        }

        public static void Raise(T @event)
        {
            isRaising = true;

            for (int i = bindings.Count - 1; i >= 0; i--)
            {
                var binding = bindings[i];
                binding.OnEvent?.Invoke(@event);
                binding.OnEventNoArgs?.Invoke();
            }

            isRaising = false;
            ApplyPending();
        }

        static void ApplyPending()
        {
            if (pendingAdds.Count > 0)
            {
                bindings.AddRange(pendingAdds);
                pendingAdds.Clear();
            }

            if (pendingRemoves.Count > 0)
            {
                foreach (var binding in pendingRemoves)
                    bindings.Remove(binding);

                pendingRemoves.Clear();
            }
        }

        internal static void Clear()
        {
            Debug.Log($"[EventBus] Clearing {typeof(T).Name} bindings ({bindings.Count})");
            bindings.Clear();
            pendingAdds.Clear();
            pendingRemoves.Clear();
        }
    }
}
