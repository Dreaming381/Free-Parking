using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace FreeParking
{
    public struct GameFlagHandle
    {
        internal Hash128 guid;
    }

    /// <summary>
    /// Fetch this from the worldBlackboardEntity to get access to all the flags in the game at runtime.
    /// </summary>
    public partial struct GameFlags : ICollectionComponent
    {
        // We only store set flags, as this ends up being the most memory efficient
        // and since we can't know about all possible flags in advance, also ends up
        // being efficient for dynamic flag location management as well.
        NativeHashSet<Hash128> m_flags;

        public GameFlags(int initialFlagsCapacity) => m_flags = new NativeHashSet<Hash128>(initialFlagsCapacity, Allocator.Persistent);

        public JobHandle TryDispose(JobHandle inputDeps)
        {
            if (m_flags.IsCreated)
                return m_flags.Dispose(inputDeps);
            return inputDeps;
        }

        public void SetFlag(GameFlagHandle flagHandle) => m_flags.Add(flagHandle.guid);
        public void ClearFlag(GameFlagHandle flagHandle) => m_flags.Remove(flagHandle.guid);
        public bool IsSet(GameFlagHandle flagHandle) => m_flags.Contains(flagHandle.guid);
    }
}

