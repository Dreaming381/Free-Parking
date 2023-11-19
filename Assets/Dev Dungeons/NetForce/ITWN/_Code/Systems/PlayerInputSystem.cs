using Latios;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NetForce.Systems
{
    [RequireMatchingQueriesForUpdate]
    public partial class PlayerInputSystem : SubSystem
    {
        PlayerInput m_input;
        EntityQuery m_query;

        protected override void OnCreate()
        {
            m_input = new PlayerInput();
            m_query = Fluent.With<PlayerTag>(true).WithAnyEnabled<ForwardDesiredActions>().Build();
        }

        protected override void OnStartRunning() => m_input.Enable();
        protected override void OnStopRunning() => m_input.Disable();

        protected override void OnUpdate()
        {
            Dependency = new InputJob
            {
                movement = m_input.CharacterActions.Movement.ReadValue<Vector2>(),
                antiSkid = m_input.CharacterActions.AntiSkid.IsPressed(),
                brake    = m_input.CharacterActions.Brake.IsPressed(),
                jump     = m_input.CharacterActions.Jump.IsPressed(),
                crouch   = m_input.CharacterActions.Crouch.IsPressed(),

                forwardDesiredActionsHandle = SystemAPI.GetComponentTypeHandle<ForwardDesiredActions>()
            }.Schedule(m_query, Dependency);
        }

        [BurstCompile]
        struct InputJob : IJobChunk
        {
            public float2 movement;
            public bool   antiSkid;
            public bool   brake;
            public bool   jump;
            public bool   crouch;

            public ComponentTypeHandle<ForwardDesiredActions> forwardDesiredActionsHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var forwardArray = chunk.GetNativeArray(ref forwardDesiredActionsHandle);
                for (int i = 0; i < forwardArray.Length; i++)
                {
                    var direction   = math.normalizesafe(movement, float2.zero);
                    forwardArray[i] = new ForwardDesiredActions
                    {
                        direction    = math.select(forwardArray[i].direction, direction, !direction.Equals(float2.zero)),
                        forwardInput = math.length(direction)
                    };
                }
            }
        }
    }
}

