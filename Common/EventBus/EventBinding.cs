using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Dessentials.Common.EventBus
{
    public interface IEventBinding<T> where T : IEvent
    {
        Action<T> OnEvent { get; set; }
        Action OnEventNoArgs { get; set; }
    }

    public class EventBinding<T> : IEventBinding<T>, IDisposable where T : IEvent
    {
        Action<T> onEvent;
        Action onEventNoArgs;
        bool disposed;

        readonly Dictionary<Func<T, UniTaskVoid>, Action<T>> taskHandlers = new();
        readonly Dictionary<Func<UniTaskVoid>, Action> taskNoArgsHandlers = new();

        Action<T> IEventBinding<T>.OnEvent
        {
            get => onEvent;
            set => onEvent = value;
        }

        Action IEventBinding<T>.OnEventNoArgs
        {
            get => onEventNoArgs;
            set => onEventNoArgs = value;
        }

        public EventBinding(Action<T> onEvent)
        {
            this.onEvent = onEvent;
        }

        public EventBinding(Action onEventNoArgs)
        {
            this.onEventNoArgs = onEventNoArgs;
        }

        public EventBinding(Func<T, UniTaskVoid> onEvent)
        {
            Add(onEvent);
        }

        public EventBinding(Func<UniTaskVoid> onEventNoArgs)
        {
            Add(onEventNoArgs);
        }

        public void Add(Action<T> handler) => onEvent += handler;
        public void Remove(Action<T> handler) => onEvent -= handler;
        public void Add(Action handler) => onEventNoArgs += handler;
        public void Remove(Action handler) => onEventNoArgs -= handler;

        public void Add(Func<T, UniTaskVoid> handler)
        {
            if (taskHandlers.ContainsKey(handler)) return;

            Action<T> wrapper = @event => handler(@event).Forget();
            taskHandlers[handler] = wrapper;
            onEvent += wrapper;
        }

        public void Remove(Func<T, UniTaskVoid> handler)
        {
            if (taskHandlers.TryGetValue(handler, out var wrapper))
            {
                onEvent -= wrapper;
                taskHandlers.Remove(handler);
            }
        }

        public void Add(Func<UniTaskVoid> handler)
        {
            if (taskNoArgsHandlers.ContainsKey(handler)) return;

            Action wrapper = () => handler().Forget();
            taskNoArgsHandlers[handler] = wrapper;
            onEventNoArgs += wrapper;
        }

        public void Remove(Func<UniTaskVoid> handler)
        {
            if (taskNoArgsHandlers.TryGetValue(handler, out var wrapper))
            {
                onEventNoArgs -= wrapper;
                taskNoArgsHandlers.Remove(handler);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            EventBus<T>.Deregister(this);
            onEvent = null;
            onEventNoArgs = null;
            taskHandlers.Clear();
            taskNoArgsHandlers.Clear();
        }
    }
}
