using Latios;
using Latios.Transforms;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CharacterAdventures
{
    [InternalBufferCapacity(2)]
    public struct TacticalGuyArmIKStats : IBufferElementData
    {
        public FixedString32Bytes         handBoneName;
        public EntityWith<WorldTransform> handTarget;
    }
}

