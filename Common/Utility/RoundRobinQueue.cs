using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dessentials.Utility
{
    public class RoundRobinQueue<T>
    {
        private Queue<T> _queue;

        public RoundRobinQueue(Queue<T> queue)
        {
            _queue = queue;
        }

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
        }

        public T Dequeue()
        {
            var result = _queue.Dequeue();
            _queue.Enqueue(result);
            return result;
        }
    }
}
