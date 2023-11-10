using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DreamingImLatios.MyriWatch.Authoring
{
    public class MyriSpawnerAuthoring : MonoBehaviour
    {
        public GameObject audioSource;
        public float      radius              = 20f;
        public float      spawnRate           = 2f;
        public float      timeUntilFirstSpawn = 1f;
    }

    public class MyriSpawnerAuthoringBaker : Baker<MyriSpawnerAuthoring>
    {
        public override void Bake(MyriSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new MyriWatchSpawner
            {
                audioEntity        = GetEntity(authoring.audioSource, TransformUsageFlags.Dynamic),
                radius             = authoring.radius,
                spawnInterval      = math.rcp(authoring.spawnRate),
                timeUntilNextSpawn = authoring.timeUntilFirstSpawn
            });
        }
    }
}

