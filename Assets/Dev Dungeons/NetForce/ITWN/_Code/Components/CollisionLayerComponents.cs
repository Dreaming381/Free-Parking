using Latios;
using Latios.Psyshock;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace NetForce
{
    struct EnvironmentTag : IComponentData { }

    partial struct EnvironmentCollisionLayer : ICollectionComponent
    {
        public CollisionLayer layer;

        public JobHandle TryDispose(JobHandle handle) => layer.IsCreated ? layer.Dispose(handle) : handle;
    }
}

