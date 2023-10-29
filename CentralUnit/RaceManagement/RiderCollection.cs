using Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RaceManagement
{
    /// <summary>
    /// Keeps track of a set of riders in starting order. New riders are always added to the last position on the grid.
    /// </summary>
    public class RiderCollection : IDictionary<Guid, Rider>
    {
        private readonly List<Guid> StartingOrder;
        private readonly Dictionary<Guid, Rider> Riders;
        public RiderCollection()
        {
            StartingOrder = new List<Guid>();
            Riders = new Dictionary<Guid, Rider>();
        }

        public Rider this[Guid key] 
        { 
            get => Riders[key];
            set => Add(key, value);
        }

        public ICollection<Guid> Keys => StartingOrder.ToList();

        public ICollection<Rider> Values => StartingOrder.Select((Guid g) => Riders[g]).ToList();

        public int Count => StartingOrder.Count;

        public bool IsReadOnly => false;

        public void Add(Rider rider)
        {
            Add(rider.Id, rider);
        }

        /// <summary>
        /// Adds a new rider to the end of the start grid at the last position.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(Guid key, Rider value)
        {
            try
            {
                Riders.Add(key, value);
            }
            catch (Exception e) when (e is ArgumentException || e is ArgumentException)
            {
                throw;
            }

            StartingOrder.Add(key);
        }

        /// <summary>
        /// Change the grid position of a rider, targetPosition is ZERO INDEXED
        /// All riders previously on targetPosition or later are shifted down one position.
        /// All riders in between the previous position of the target rider and targetPositon are shifted on position up
        /// </summary>
        /// <param name="riderId"></param>
        /// <param name="targetPosition"></param>
        public void ChangePosition(Guid riderId, int targetPosition)
        {
            if (!Riders.ContainsKey(riderId))
            {
                throw new KeyNotFoundException($"Rider {riderId} not found in collection");
            }

            if (targetPosition < 0)
            {
                throw new ArgumentException($"Cannot assign a rider to a negative starting position");
            }

            if(targetPosition > StartingOrder.Count)
            {
                throw new ArgumentException($"Cannot assign a rider to a starting position more than the number of riders, that would leave a gap in the grid");
            }

            StartingOrder.Remove(riderId);
            StartingOrder.Insert(targetPosition, riderId);
        }

        public void Add(KeyValuePair<Guid, Rider> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            Riders.Clear();
            StartingOrder.Clear();
        }

        public bool Contains(KeyValuePair<Guid, Rider> item)
        {
            return Riders.ContainsKey(item.Key);
        }

        public bool ContainsKey(Guid key)
        {
            return Riders.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<Guid, Rider>[] array, int arrayIndex)
        {
            int sourceIndex = 0;
            for(int targetIndex = arrayIndex; targetIndex<array.Length && targetIndex <StartingOrder.Count; targetIndex++)
            {
                Guid riderId = StartingOrder[sourceIndex++];
                array[targetIndex] = new KeyValuePair<Guid, Rider>(riderId, Riders[riderId]);
            }
        }

        public IEnumerator<KeyValuePair<Guid, Rider>> GetEnumerator()
        {
            return StartingOrder.Select((Guid id) => new KeyValuePair<Guid, Rider>(id, Riders[id])).GetEnumerator();
        }

        public bool Remove(Guid key)
        {
            bool shouldRemove = Riders.Remove(key);

            if (shouldRemove)
            {
                StartingOrder.Remove(key);
            }

            return shouldRemove;
        }

        public bool Remove(KeyValuePair<Guid, Rider> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValue(Guid key, out Rider value)
        {
            bool success = Riders.TryGetValue(key, out value);

            return success;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
