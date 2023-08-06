using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace FreeParking.Bootstrap.Systems
{
    public partial class ProcessBootstrapInputCommandsSystem : SubSystem
    {
        InputGlobalActions m_actions;

        protected override void OnCreate()
        {
            m_actions = new InputGlobalActions();
            m_actions.Enable();
            m_actions.GlobalActionsMap.Enable();
        }

        protected override void OnDestroy()
        {
            m_actions.GlobalActionsMap.Disable();
            m_actions.Disable();
            m_actions.Dispose();
        }

        protected override void OnUpdate()
        {
            bool hitPause = m_actions.GlobalActionsMap.Pause.WasPerformedThisFrame();
            bool hitLeave = m_actions.GlobalActionsMap.ForceLeaveDevDungeon.WasPerformedThisFrame();

            if (!hitPause && !hitLeave)
                return;

            if (hitPause)
            {
                if (sceneBlackboardEntity.HasComponent<PausedTag>())
                    sceneBlackboardEntity.RemoveComponent<PausedTag>();
                else
                    sceneBlackboardEntity.AddComponent<PausedTag>();
            }
            if (hitLeave)
            {
                var currentScene = worldBlackboardEntity.GetComponentData<CurrentScene>();
                if (currentScene.current != "Main World Scene")
                    sceneBlackboardEntity.AddComponentData(new RequestLoadScene { newScene = "Main World Scene" });
            }
        }
    }
}

