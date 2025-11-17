using System;

namespace TurnBasedStrategyFramework.Common.Pathfinding.DataStructures
{
    /// <summary>
    /// Represents an item in the priority queue, consisting of the item itself and its associated priority value.
    /// </summary>
    public readonly struct PriorityQueueItem<T> : IComparable<PriorityQueueItem<T>>
    {
        /// <summary>
        /// The item stored in the priority queue.
        /// </summary>
        public readonly T Item;

        /// <summary>
        /// The priority value associated with the item.
        /// </summary>
        public readonly float Priority;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityQueueItem{T}"/> struct.
        /// </summary>
        /// <param name="item">The item to be stored in the priority queue.</param>
        /// <param name="priority">The priority value of the item.</param>
        public PriorityQueueItem(T item, float priority)
        {
            Item = item;
            Priority = priority;
        }

        /// <summary>
        /// Compares the current priority queue item with another to determine their relative order based on priority.
        /// </summary>
        /// <param name="other">The other priority queue item to compare against.</param>
        /// <returns>
        /// A value less than zero if this item has a lower priority than the other;
        /// zero if the priorities are equal; greater than zero if this item has a higher priority.
        /// </returns>
        public int CompareTo(PriorityQueueItem<T> other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }
}