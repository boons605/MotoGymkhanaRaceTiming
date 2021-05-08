using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RaceManagement
{
    public class IndexedQueue<TKey, TValue> : IEnumerable<TValue>
    {
        private LinkedList<TValue> data = new LinkedList<TValue>();
        private Dictionary<TKey, LinkedListNode<TValue>> index = new Dictionary<TKey, LinkedListNode<TValue>>();

        Func<TValue, TKey> toIndex;

        public int Count => data.Count;

        /// <summary>
        /// This is a FIFO queue that also supports removing values in the middle. To ensure O(1) operations all values must be indexed by a key
        /// </summary>
        /// <param name="toIndex">Transforms values into unique keys.</param>
        public IndexedQueue(Func<TValue, TKey> toIndex)
        {
            this.toIndex = toIndex;
        }

        /// <summary>
        /// Adds an item to the queue. Throws when an item with the same key already exists
        /// </summary>
        /// <param name="value"></param>
        public void Enqueue(TValue value)
        {
            LinkedListNode<TValue> node = new LinkedListNode<TValue>(value);
            index.Add(toIndex(value), node);
            data.AddLast(node);
        }

        /// <summary>
        /// Removes an item from the head of the queue
        /// </summary>
        /// <returns></returns>
        public TValue Dequeue()
        {
            LinkedListNode<TValue> first = data.First;
            data.RemoveFirst();

            index.Remove(toIndex(first.Value));

            return first.Value;
        }

        /// <summary>
        /// Removes the item with the corresponding key from the queue, regardless of its position. Throws if key is not in the index
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue Remove (TKey key)
        {
            LinkedListNode<TValue> node = this.index[key];

            data.Remove(node);

            return node.Value;
        }

        /// <summary>
        /// Removes the item from the queue, regardless of its position. Throws if value is not in the queue
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue Remove(TValue value) => Remove(toIndex(value));

        public bool Contains(TKey key) => index.ContainsKey(key);
        public bool Contains(TValue value) => index.ContainsKey(toIndex(value));


        public IEnumerator<TValue> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }
    }
}
