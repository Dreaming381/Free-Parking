using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Latios
{
    // Todo: Make public once finished.
    internal struct DynamicHashMap<TKey, TValue> where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
    {
        /// <summary>
        /// Construct a DynamicHashMap instance that uses the passed in DynamicBuffer as a backing storage
        /// </summary>
        /// <param name="buffer">The dynamic buffer that contains the underlying data for the DynamicHashMap</param>
        public DynamicHashMap(DynamicBuffer<Pair> buffer)
        {
            m_buffer = buffer;
        }

        /// <summary>
        /// Implicitly converts the DynamicBuffer into the corresponding DynamicHashMap
        /// </summary>
        public static implicit operator DynamicHashMap<TKey, TValue>(DynamicBuffer<Pair> buffer)
        {
            return new DynamicHashMap<TKey, TValue>(buffer);
        }

        /// <summary>
        /// True if this DynamicHashMap is backed by a valid DynamicBuffer. False otherwise.
        /// </summary>
        public bool isCreated => m_buffer.IsCreated;

        /// <summary>
        /// The number of key-value pairs in the hashmap.
        /// </summary>
        public int count
        {
            get
            {
                if (isEmpty)
                    return 0;
                return m_buffer[m_buffer.Length - 1].nextIndex;
            }
        }

        /// <summary>
        /// True if the hashmap is empty.
        /// </summary>
        public bool isEmpty => m_buffer.IsEmpty || count == 0;

        /// <summary>
        /// Removes all elements from the hashmap.
        /// </summary>
        public void Clear() => m_buffer.Clear();

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks>If the key is already present, this method returns false without modifying the hash map.</remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>True if the key-value pair was added.</returns>
        public bool TryAdd(in TKey key, in TValue value)
        {
            if (m_buffer.Capacity == 0)
                m_buffer.EnsureCapacity(2);

            var     bucket    = GetBucket(in key);
            ref var candidate = ref m_buffer.ElementAt(bucket);
            if (candidate.isOccupied)
            {
                for (int safetyBreakout = 0; safetyBreakout < m_buffer.Length; safetyBreakout++)
                {
                    if (candidate.key.Equals(key))
                        return false;

                    if (candidate.nextIndex == 0 || bucket >= m_buffer.Length - 1)
                    {
                        ref var last   = ref m_buffer.ElementAt(m_buffer.Length - 1);
                        var     total  = last.nextIndex;
                        last.nextIndex = 0;
                        if (m_buffer.Length == m_buffer.Capacity)
                        {
                            // Todo: Realloc

                            // Start over
                            return TryAdd(in key, in value);
                        }
                        candidate.nextIndex         = m_buffer.Length;
                        m_buffer.Add(new Pair { key = key, value = value, meta = (uint)total | 0x10000000 });
                        IncrementCount();
                        return true;
                    }
                    candidate = ref m_buffer.ElementAt(candidate.nextIndex);
                }

                UnityEngine.Debug.LogError(
                    "DynamicHashMap is corrupted and has circular references. Either the buffer is being wrongly interpreted or this is a Latios Framework bug.");
                return false;
            }
            candidate.key        = key;
            candidate.value      = value;
            candidate.isOccupied = true;
            IncrementCount();
            return true;
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if a key-value pair was removed.</returns>
        public bool Remove(in TKey key)
        {
            if (isEmpty)
                return false;

            var     bucket    = GetBucket(in key);
            ref var candidate = ref m_buffer.ElementAt(bucket);
            if (candidate.isOccupied)
            {
                if (candidate.key.Equals(key))
                {
                    if (candidate.nextIndex != 0)
                    {
                        var     indexToBackfill = candidate.nextIndex;
                        ref var replacement     = ref m_buffer.ElementAt(indexToBackfill);
                        if (candidate.nextIndex == m_buffer.Length)
                            replacement.nextIndex = 0;
                        candidate                 = replacement;
                        Backfill(indexToBackfill);
                        DecrementCount();
                        return true;
                    }
                    candidate.key        = default;
                    candidate.value      = default;
                    candidate.isOccupied = false;
                    DecrementCount();
                    return true;
                }

                if (candidate.nextIndex == 0)
                    return false;

                for (int safetyBreakout = 0; safetyBreakout < m_buffer.Length; safetyBreakout++)
                {
                    ref var previousCandidate = ref candidate;
                    candidate                 = ref m_buffer.ElementAt(candidate.nextIndex);

                    if (candidate.key.Equals(key))
                    {
                        var indexToBackfill = previousCandidate.nextIndex;
                        if (previousCandidate.nextIndex == m_buffer.Length)
                            previousCandidate.nextIndex = 0;
                        else
                            previousCandidate.nextIndex = candidate.nextIndex;
                        // Todo: Backfill

                        return true;
                    }

                    if (candidate.nextIndex == 0 || bucket >= m_buffer.Length - 1)
                    {
                        return false;
                    }
                }

                UnityEngine.Debug.LogError(
                    "DynamicHashMap is corrupted and has circular references. Either the buffer is being wrongly interpreted or this is a Latios Framework bug.");
                return false;
            }
            return false;
        }

        public struct Pair
        {
            internal uint   meta;
            internal TKey   key;
            internal TValue value;

            internal bool isOccupied
            {
                get => (meta & 0x10000000) != 0;
                set => meta = (meta & 0x7fffffff) | math.select(0u, 1u, value) << 31;
            }

            internal int nextIndex
            {
                get => (int)(meta & 0x7fffffff);
                set => meta = (meta & 0x80000000) | (uint)value;
            }
        }

        DynamicBuffer<Pair> m_buffer;

        int GetBucket(in TKey key)
        {
            var mask = (m_buffer.Capacity / 2) - 1;
            return key.GetHashCode() & mask;
        }

        void IncrementCount()
        {
            m_buffer.ElementAt(m_buffer.Length - 1).nextIndex++;
        }

        void DecrementCount()
        {
            m_buffer.ElementAt(m_buffer.Length - 1).nextIndex--;
        }

        void Backfill(int index)
        {
            if (index == m_buffer.Length - 1)
            {
                // Assume that we don't need to correct the thing pointing to this
                var total = m_buffer.ElementAt(index).nextIndex;
                m_buffer.Length--;
                m_buffer.ElementAt(index - 1).nextIndex = total;
                return;
            }

            ref var elementToFill = ref m_buffer.ElementAt(index);
            ref var elementToMove = ref m_buffer.ElementAt(m_buffer.Length - 1);
            // Find the thing pointing to the element we are going to move, so that we can patch it.
            ref var candidate = ref m_buffer.ElementAt(GetBucket(in elementToMove.key));
            for (int i = 0; i < m_buffer.Length; i++)
            {
                if (candidate.nextIndex == m_buffer.Length - 1)
                {
                    candidate.nextIndex     = index;
                    elementToFill           = elementToMove;
                    elementToFill.nextIndex = 0;
                    Backfill(m_buffer.Length - 1);
                    return;
                }
                candidate = ref m_buffer.ElementAt(candidate.nextIndex);
            }

            UnityEngine.Debug.LogError(
                "DynamicHashMap is corrupted and has circular references. Either the buffer is being wrongly interpreted or this is a Latios Framework bug.");
        }

        void ReallocUp(int newCapacity)
        {
            // Todo:
        }
    }
}

