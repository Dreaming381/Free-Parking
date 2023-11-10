using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DreamingImLatios.MyriWatch
{
    struct MyriWatchSpawner : IComponentData
    {
        public EntityWith<Prefab> audioEntity;
        public float              radius;
        public float              spawnInterval;
        public float              timeUntilNextSpawn;
    }
}

