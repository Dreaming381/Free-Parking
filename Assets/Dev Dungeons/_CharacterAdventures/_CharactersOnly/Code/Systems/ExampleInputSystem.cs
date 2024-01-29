using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace CharacterAdventures.Systems
{
    [RequireMatchingQueriesForUpdate]
    public partial class ExampleInputSystem : SubSystem
    {
        ExampleInputActions m_input;

        protected override void OnCreate()
        {
            m_input = new ExampleInputActions();
            m_input.Disable();
        }

        protected override void OnStartRunning() => m_input.Enable();
        protected override void OnStopRunning() => m_input.Disable();
        protected override void OnDestroy() => m_input.Dispose();

        protected override void OnUpdate()
        {
            foreach (var input in SystemAPI.Query<RefRW<ExampleDesiredActions> >())
            {
                input.ValueRW = new ExampleDesiredActions
                {
                    move = m_input.ExampleMap.Movement.ReadValue<UnityEngine.Vector2>()
                };
            }
        }
    }
}

