using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Todo: XML Documentation

namespace Latios.Kinemation
{
    public partial struct GraphicsBufferBrokerReference : IManagedStructComponent
    {
        public GraphicsBufferBroker graphicsBufferBroker { get; internal set; }

        public void Dispose()
        {
            if (graphicsBufferBroker != null)
                graphicsBufferBroker.Dispose();
        }
    }

    public static class DeformationGraphicsBufferBrokerExtensions
    {
        #region Public Access
        public static GraphicsBuffer GetSkinningTransformsBuffer(this GraphicsBufferBroker broker) => broker.GetPersistentBufferNoResize(s_skinningTransformsID);
        public static GraphicsBuffer GetDeformedVerticesBuffer(this GraphicsBufferBroker broker) => broker.GetPersistentBufferNoResize(s_deformedVerticesID);
        public static GraphicsBuffer GetMeshVerticesBuffer(this GraphicsBufferBroker broker) => broker.GetPersistentBufferNoResize(s_meshVerticesID);

        public static GraphicsBuffer GetMetaUint3UploadBuffer(this GraphicsBufferBroker broker, uint requiredNumUint3s)
        {
            requiredNumUint3s = math.max(requiredNumUint3s, kMinUploadMetaSize);
            return broker.GetUploadBuffer(s_metaUint3UploadID, requiredNumUint3s * 3);
        }
        public static GraphicsBuffer GetMetaUint4UploadBuffer(this GraphicsBufferBroker broker, uint requiredNumUint4s)
        {
            requiredNumUint4s = math.max(requiredNumUint4s, kMinUploadMetaSize);
            return broker.GetUploadBuffer(s_metaUint3UploadID, requiredNumUint4s * 4);
        }
        #endregion

        #region Reservations
        internal static GraphicsBufferBroker.StaticID s_skinningTransformsID = GraphicsBufferBroker.ReservePersistentBuffer();
        internal static GraphicsBufferBroker.StaticID s_deformedVerticesID   = GraphicsBufferBroker.ReservePersistentBuffer();
        internal static GraphicsBufferBroker.StaticID s_meshVerticesID       = GraphicsBufferBroker.ReservePersistentBuffer();
        internal static GraphicsBufferBroker.StaticID s_meshWeightsID        = GraphicsBufferBroker.ReservePersistentBuffer();
        internal static GraphicsBufferBroker.StaticID s_meshBindPosesID      = GraphicsBufferBroker.ReservePersistentBuffer();
        internal static GraphicsBufferBroker.StaticID s_meshBlendShapesID    = GraphicsBufferBroker.ReservePersistentBuffer();
        internal static GraphicsBufferBroker.StaticID s_boneOffsetsID        = GraphicsBufferBroker.ReservePersistentBuffer();

        internal static GraphicsBufferBroker.StaticID s_metaUint3UploadID = GraphicsBufferBroker.ReserveUploadPool();
        internal static GraphicsBufferBroker.StaticID s_metaUint4UploadID = GraphicsBufferBroker.ReserveUploadPool();

        internal static GraphicsBufferBroker.StaticID s_meshVerticesUploadID    = GraphicsBufferBroker.ReserveUploadPool();
        internal static GraphicsBufferBroker.StaticID s_meshWeightsUploadID     = GraphicsBufferBroker.ReserveUploadPool();
        internal static GraphicsBufferBroker.StaticID s_meshBindPosesUploadID   = GraphicsBufferBroker.ReserveUploadPool();
        internal static GraphicsBufferBroker.StaticID s_meshBlendShapesUploadID = GraphicsBufferBroker.ReserveUploadPool();
        internal static GraphicsBufferBroker.StaticID s_boneOffsetsUploadID     = GraphicsBufferBroker.ReserveUploadPool();
        internal static GraphicsBufferBroker.StaticID s_bonesUploadID           = GraphicsBufferBroker.ReserveUploadPool();

        const uint kMinUploadMetaSize = 128;
        #endregion
    }

    public class GraphicsBufferBroker : IDisposable
    {
        #region API
        public struct StaticID
        {
            internal int index;
        }

        public static StaticID ReservePersistentBuffer() => new StaticID
        {
            index = s_reservedPersistentBuffersCount++
        };
        public static StaticID ReserveUploadPool() => new StaticID
        {
            index = s_reservedUploadPoolsCount++
        };

        public void InitializePersistentBuffer(StaticID staticID, uint initialNumElements, uint strideOfElement, GraphicsBuffer.Target bindingTarget, ComputeShader copyShader)
        {
            while (m_persistentBuffers.Count <= staticID.index)
                m_persistentBuffers.Add(default);
            m_persistentBuffers[staticID.index] = new PersistentBuffer(initialNumElements, strideOfElement, bindingTarget, copyShader, m_buffersToDelete);
        }

        public GraphicsBuffer GetPersistentBuffer(StaticID staticID, uint requiredNumElements)
        {
            var persistent                      = m_persistentBuffers[staticID.index];
            var result                          = persistent.GetBuffer(requiredNumElements, m_frameFenceTracker.CurrentFrameId);
            m_persistentBuffers[staticID.index] = persistent;
            return result;
        }

        public GraphicsBuffer GetPersistentBufferNoResize(StaticID staticID) => m_persistentBuffers[staticID.index].GetBufferNoResize();

        public void InitializeUploadPool(StaticID staticID, uint strideOfElement, GraphicsBuffer.Target bindingTarget)
        {
            while (m_uploadPools.Count <= staticID.index)
                m_uploadPools.Add(default);
            m_uploadPools[staticID.index] = new UploadPool(strideOfElement, bindingTarget);
        }

        public GraphicsBuffer GetUploadBuffer(StaticID staticID, uint requiredNumElements)
        {
            var upload                    = m_uploadPools[staticID.index];
            var result                    = upload.GetBuffer(requiredNumElements, m_frameFenceTracker.CurrentFrameId);
            m_uploadPools[staticID.index] = upload;
            return result;
        }
        #endregion

        #region Members
        static int s_reservedPersistentBuffersCount = 0;
        static int s_reservedUploadPoolsCount       = 0;

        List<PersistentBuffer>           m_persistentBuffers = new List<PersistentBuffer>();
        List<UploadPool>                 m_uploadPools       = new List<UploadPool>();
        List<BufferQueuedForDestruction> m_buffersToDelete   = new List<BufferQueuedForDestruction>();
        FrameFenceTracker                m_frameFenceTracker = new FrameFenceTracker(true);
        #endregion

        #region Buffer Types
        struct PersistentBuffer : IDisposable
        {
            GraphicsBuffer                   m_currentBuffer;
            ComputeShader                    m_copyShader;
            List<BufferQueuedForDestruction> m_destructionQueue;
            uint                             m_currentSize;
            uint                             m_stride;
            GraphicsBuffer.Target            m_bindingTarget;

            public PersistentBuffer(uint initialSize, uint stride, GraphicsBuffer.Target bufferType, ComputeShader copyShader, List<BufferQueuedForDestruction> destructionQueue)
            {
                uint size          = math.ceilpow2(initialSize);
                m_currentBuffer    = new GraphicsBuffer(bufferType, GraphicsBuffer.UsageFlags.None, (int)size, (int)stride);
                m_copyShader       = copyShader;
                m_destructionQueue = destructionQueue;
                m_currentSize      = size;
                m_stride           = stride;
                m_bindingTarget    = bufferType;
            }

            public void Dispose()
            {
                m_currentBuffer.Dispose();
            }

            public bool valid => m_currentBuffer != null;

            public GraphicsBuffer GetBufferNoResize() => m_currentBuffer;

            public GraphicsBuffer GetBuffer(uint requiredSize, uint frameId)
            {
                //UnityEngine.Debug.Log($"Requested Persistent Buffer of size: {requiredSize} while currentSize is: {m_currentSize}");
                if (requiredSize <= m_currentSize)
                    return m_currentBuffer;

                uint size = math.ceilpow2(requiredSize);
                if (requiredSize * m_stride > 1024 * 1024 * 1024)
                    Debug.LogWarning("Attempted to allocate a mesh deformation buffer over 1 GB. Rendering artifacts may occur.");
                if (requiredSize * m_stride < 1024 * 1024 * 1024 && size * m_stride > 1024 * 1024 * 1024)
                    size        = 1024 * 1024 * 1024 / m_stride;
                var prevBuffer  = m_currentBuffer;
                m_currentBuffer = new GraphicsBuffer(m_bindingTarget, GraphicsBuffer.UsageFlags.None, (int)size, (int)m_stride);
                if (m_copyShader != null)
                {
                    m_copyShader.GetKernelThreadGroupSizes(0, out var threadGroupSize, out _, out _);
                    m_copyShader.SetBuffer(0, "_dst", m_currentBuffer);
                    m_copyShader.SetBuffer(0, "_src", prevBuffer);
                    uint copySize = m_bindingTarget == GraphicsBuffer.Target.Raw ? m_currentSize / 4 : m_currentSize;
                    for (uint dispatchesRemaining = copySize / threadGroupSize, start = 0; dispatchesRemaining > 0;)
                    {
                        uint dispatchCount = math.min(dispatchesRemaining, 65535);
                        m_copyShader.SetInt("_start", (int)(start * threadGroupSize));
                        m_copyShader.Dispatch(0, (int)dispatchCount, 1, 1);
                        dispatchesRemaining -= dispatchCount;
                        start               += dispatchCount;
                        //UnityEngine.Debug.Log($"Dispatched buffer type: {m_bindingTarget} with dispatchCount: {dispatchCount}");
                    }
                }
                m_currentSize                                                  = size;
                m_destructionQueue.Add(new BufferQueuedForDestruction { buffer = prevBuffer, frameId = frameId });
                return m_currentBuffer;
            }
        }

        struct BufferQueuedForDestruction
        {
            public GraphicsBuffer buffer;
            public uint           frameId;
        }

        struct UploadPool : IDisposable
        {
            struct TrackedBuffer
            {
                public GraphicsBuffer buffer;
                public uint           size;
                public uint           frameId;
            }

            uint                  m_stride;
            GraphicsBuffer.Target m_type;
            List<TrackedBuffer>   m_buffersInPool;
            List<TrackedBuffer>   m_buffersInFlight;

            public UploadPool(uint stride, GraphicsBuffer.Target bufferType)
            {
                m_stride          = stride;
                m_type            = bufferType;
                m_buffersInPool   = new List<TrackedBuffer>();
                m_buffersInFlight = new List<TrackedBuffer>();
            }

            public bool valid => m_buffersInPool != null;

            public GraphicsBuffer GetBuffer(uint requiredSize, uint frameId)
            {
                for (int i = 0; i < m_buffersInPool.Count; i++)
                {
                    if (m_buffersInPool[i].size >= requiredSize)
                    {
                        var tracked     = m_buffersInPool[i];
                        tracked.frameId = frameId;
                        m_buffersInFlight.Add(tracked);
                        m_buffersInPool.RemoveAtSwapBack(i);
                        return tracked.buffer;
                    }
                }

                if (m_buffersInPool.Count > 0)
                {
                    m_buffersInPool[0].buffer.Dispose();
                    m_buffersInPool.RemoveAtSwapBack(0);
                }

                uint size       = math.ceilpow2(requiredSize);
                var  newTracked = new TrackedBuffer
                {
                    buffer  = new GraphicsBuffer(m_type, GraphicsBuffer.UsageFlags.LockBufferForWrite, (int)size, (int)m_stride),
                    size    = size,
                    frameId = frameId
                };
                m_buffersInFlight.Add(newTracked);
                return newTracked.buffer;
            }

            public void CollectFinishedBuffers(uint finishedFrameId)
            {
                for (int i = 0; i < m_buffersInFlight.Count; i++)
                {
                    var tracked = m_buffersInFlight[i];
                    if (IsEqualOrNewer(finishedFrameId, tracked.frameId))
                    {
                        m_buffersInPool.Add(tracked);
                        m_buffersInFlight.RemoveAtSwapBack(i);
                        i--;
                    }
                }
            }

            public void Dispose()
            {
                foreach (var buffer in m_buffersInPool)
                    buffer.buffer.Dispose();
                foreach (var buffer in m_buffersInFlight)
                    buffer.buffer.Dispose();
            }
        }

        struct FrameFenceTracker
        {
            uint m_currentFrameId;
            uint m_recoveredFrameId;
            int  m_numberOfFramesToWait;

            public uint CurrentFrameId => m_currentFrameId;
            public uint RecoveredFrameId => m_recoveredFrameId;

            public FrameFenceTracker(bool dummy)
            {
                m_numberOfFramesToWait = Unity.Rendering.SparseUploader.NumFramesInFlight;
                m_currentFrameId       = (uint)m_numberOfFramesToWait;
                m_recoveredFrameId     = 0;
            }

            public void Update()
            {
                m_recoveredFrameId++;
                m_currentFrameId++;
            }
        }
        #endregion

        #region Internal Management Methods
        static bool IsEqualOrNewer(uint potentiallyNewer, uint requiredVersion)
        {
            return ((int)(potentiallyNewer - requiredVersion)) >= 0;
        }

        internal void Update()
        {
            m_frameFenceTracker.Update();
            for (int i = 0; i < m_uploadPools.Count; i++)
            {
                var pool = m_uploadPools[i];
                if (pool.valid)
                {
                    pool.CollectFinishedBuffers(m_frameFenceTracker.RecoveredFrameId);
                    m_uploadPools[i] = pool;
                }
            }

            for (int i = 0; i < m_buffersToDelete.Count; i++)
            {
                if (IsEqualOrNewer(m_frameFenceTracker.RecoveredFrameId, m_buffersToDelete[i].frameId))
                {
                    m_buffersToDelete[i].buffer.Dispose();
                    m_buffersToDelete.RemoveAtSwapBack(i);
                    i--;
                }
            }
        }

        public void Dispose()
        {
            foreach (var b in m_buffersToDelete)
                b.buffer.Dispose();
            foreach (var b in m_uploadPools)
            {
                if (b.valid)
                    b.Dispose();
            }
            foreach (var b in m_persistentBuffers)
            {
                if (b.valid)
                    b.Dispose();
            }
        }
        #endregion
    }
}

