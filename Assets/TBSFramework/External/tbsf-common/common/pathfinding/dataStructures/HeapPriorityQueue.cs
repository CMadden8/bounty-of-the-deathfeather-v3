using System.Collections.Generic;

namespace TurnBasedStrategyFramework.Common.Pathfinding.DataStructures
{
    /// <summary>
    /// Represents a heap-based priority queue.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the priority queue.</typeparam>
    class HeapPriorityQueue<T> : IPriorityQueue<T>
    {
        private List<PriorityQueueItem<T>> _queue;

        public HeapPriorityQueue(int initialCapacity = 0)
        {
            _queue = new List<PriorityQueueItem<T>>(initialCapacity);
        }

        public int Count
        {
            get { return _queue.Count; }
        }

        public void Enqueue(T item, float priority)
        {
            _queue.Add(new PriorityQueueItem<T>(item, priority));
            int ci = _queue.Count - 1;
            while (ci > 0)
            {
                int pi = (ci - 1) / 2;
                if (_queue[ci].CompareTo(_queue[pi]) >= 0)
                    break;
                (_queue[pi], _queue[ci]) = (_queue[ci], _queue[pi]);
                ci = pi;
            }
        }
        public T Dequeue()
        {
            int li = _queue.Count - 1;
            var frontItem = _queue[0];
            _queue[0] = _queue[li];
            _queue.RemoveAt(li);

            --li;
            int pi = 0;
            while (true)
            {
                int ci = pi * 2 + 1;
                if (ci > li) break;
                int rc = ci + 1;
                if (rc <= li && _queue[rc].CompareTo(_queue[ci]) < 0)
                    ci = rc;
                if (_queue[pi].CompareTo(_queue[ci]) <= 0) break;
                (_queue[ci], _queue[pi]) = (_queue[pi], _queue[ci]);
                pi = ci;
            }
            return frontItem.Item;
        }
    }
}