using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace FreeParking.MainWorld.MainGameplay.Systems
{
    [RequireMatchingQueriesForUpdate]
    public partial class PlayerInputSystem : SubSystem
    {
        PlayerInput m_input;
        EntityQuery m_query;

        protected override void OnCreate()
        {
            m_input = new PlayerInput();
            m_query = Fluent.WithAny<PlayerMotionDesiredActions>().WithAny<PlayerInteractionDesiredActions>().Build();
        }

        protected override void OnStartRunning() => m_input.Enable();
        protected override void OnStopRunning() => m_input.Disable();

        protected override void OnUpdate()
        {
            Dependency = new InputJob
            {
                movement = m_input.RoamActions.Movement.ReadValue<Vector2>(),
                interact = m_input.RoamActions.Interact.IsPressed(),
                cancel   = m_input.RoamActions.Interact.IsPressed(),

                playerMotionDesiredActionsHandle      = SystemAPI.GetComponentTypeHandle<PlayerMotionDesiredActions>(),
                playerInteractionDesiredActionsHandle = SystemAPI.GetComponentTypeHandle<PlayerInteractionDesiredActions>()
            }.Schedule(m_query, Dependency);
        }

        [BurstCompile]
        struct InputJob : IJobChunk
        {
            public float2 movement;
            public bool   interact;
            public bool   cancel;

            public ComponentTypeHandle<PlayerMotionDesiredActions>      playerMotionDesiredActionsHandle;
            public ComponentTypeHandle<PlayerInteractionDesiredActions> playerInteractionDesiredActionsHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var motionArray = chunk.GetNativeArray(ref playerMotionDesiredActionsHandle);
                for (int i = 0; i < motionArray.Length; i++)
                {
                    motionArray[i] = new PlayerMotionDesiredActions
                    {
                        cameraRelativeMovement = movement,
                    };
                }
                var interactionArray = chunk.GetNativeArray(ref playerInteractionDesiredActionsHandle);
                for (int i = 0; i < interactionArray.Length; i++)
                {
                    interactionArray[i] = new PlayerInteractionDesiredActions
                    {
                        interact = interact,
                        cancel   = cancel
                    };
                }
            }
        }
    }
}

