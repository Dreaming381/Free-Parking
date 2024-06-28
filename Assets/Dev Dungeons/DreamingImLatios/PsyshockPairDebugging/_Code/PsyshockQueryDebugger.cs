using Latios.Psyshock;
using Latios.Transforms;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;

[UnityEngine.ExecuteAlways]
public class PsyshockQueryDebugger : UnityEngine.MonoBehaviour
{
    [UnityEngine.TextArea]
    public string hexString;

    // Update is called once per frame
    void Update()
    {
        if (hexString != null && hexString.Length > 2)
        {
            if (hexString[1] == '1')
                DebugDistanceBetweenColliderCollider();
        }
    }

    void DebugDistanceBetweenColliderCollider()
    {
        HexReader reader = new HexReader(hexString);
        if (reader.ReadByte() != 1)
        {
            UnityEngine.Debug.Log("Reader failed to parse first byte.");
            return;
        }
        var colliderA  = reader.ReadCollider();
        var transformA = reader.ReadTransform();

        var colliderB = reader.ReadCollider();

        var transformB  = reader.ReadTransform();
        var maxDistance = reader.ReadFloat();

        UnityEngine.Debug.Log("Debugging DistanceBetween Collider Collider");
        UnityEngine.Debug.Log($"A type: {colliderA.type}, B type: {colliderB.type}");
        UnityEngine.Debug.Log($"A transform, rotation: {transformA.rotation}, position: {transformA.position}, stretch: {transformA.stretch}, scale: {transformA.scale}");
        UnityEngine.Debug.Log($"B transform, rotation: {transformB.rotation}, position: {transformB.position}, stretch: {transformB.stretch}, scale: {transformB.scale}");

        PhysicsDebug.DrawCollider(colliderA, transformA, UnityEngine.Color.red,  24);
        PhysicsDebug.DrawCollider(colliderB, transformB, UnityEngine.Color.blue, 24);

        var hit = Physics.DistanceBetween(colliderA, transformA, colliderB, transformB, maxDistance, out var result);

        if (!hit)
        {
            Physics.DistanceBetween(colliderA, transformA, colliderB, transformB, float.MaxValue, out result);
        }

        UnityEngine.Debug.Log(
            $"\nHit: {hit}\tDistance: {result.distance}\nHitpoint A: {result.hitpointA}\tHitpoint B: {result.hitpointB}\nNormal A: {result.normalA},\tNormal B: {result.normalB},\nSubcolliders {result.subColliderIndexA} {result.subColliderIndexB}");

        UnityEngine.Debug.DrawLine(result.hitpointA, result.hitpointA + result.normalA, UnityEngine.Color.magenta);
        UnityEngine.Debug.DrawLine(result.hitpointB, result.hitpointB + result.normalB, UnityEngine.Color.cyan);

        if (colliderA.type == ColliderType.Convex)
        {
            ConvexCollider convex = colliderA;
            convex.convexColliderBlob.Dispose();
        }
        else if (colliderA.type == ColliderType.Compound)
        {
            CompoundCollider compound = colliderA;
            compound.compoundColliderBlob.Dispose();
        }

        if (colliderB.type == ColliderType.Convex)
        {
            ConvexCollider convex = colliderB;
            convex.convexColliderBlob.Dispose();
        }
        else if (colliderB.type == ColliderType.Compound)
        {
            CompoundCollider compound = colliderB;
            compound.compoundColliderBlob.Dispose();
        }
    }

    internal unsafe struct HexReader : BinaryReader
    {
        public string                 content;
        private NativeReference<long> position;

        public HexReader(string hexString)
        {
            content  = hexString;
            position = new NativeReference<long>(Allocator.Temp);
        }

        public int Length => content.Length;

        public long Position { get => position.Value; set => position.Value = value; }

        public void Dispose()
        {
            //content.Dispose();
        }

        public void ReadBytes(void* data, int bytes)
        {
            var bytePtr = (byte*)data;
            for (int i = 0; i < bytes; i++)
            {
                byte high = NibbleFromChar(content[2 * (int)Position]);
                byte low  = NibbleFromChar(content[2 * (int)Position + 1]);
                *bytePtr  = (byte)((high << 4) | low);
                bytePtr++;
                Position++;
            }

            static byte NibbleFromChar(char c)
            {
                return c switch
                       {
                           '0' => 0,
                           '1' => 1,
                           '2' => 2,
                           '3' => 3,
                           '4' => 4,
                           '5' => 5,
                           '6' => 6,
                           '7' => 7,
                           '8' => 8,
                           '9' => 9,
                           'a' => 10,
                           'b' => 11,
                           'c' => 12,
                           'd' => 13,
                           'e' => 14,
                           'f' => 15,
                           _ => 0,
                       };
            }
        }
    }
}

public static class BinaryReaderExtensions
{
    public unsafe static Collider ReadCollider<T>(this ref T reader) where T : struct, BinaryReader
    {
        Collider collider = default;
        reader.ReadBytes(UnsafeUtility.AddressOf(ref collider), UnsafeUtility.SizeOf<Collider>());
        if (collider.type == ColliderType.Convex)
        {
            ConvexCollider convex     = collider;
            convex.convexColliderBlob = reader.Read<ConvexColliderBlob>();
            collider                  = convex;
        }
        else if (collider.type == ColliderType.Compound)
        {
            CompoundCollider compound     = collider;
            compound.compoundColliderBlob = reader.Read<CompoundColliderBlob>();
            collider                      = compound;
        }
        return collider;
    }

    public unsafe static TransformQvvs ReadTransform<T>(this ref T reader) where T : struct, BinaryReader
    {
        TransformQvvs transform = default;
        reader.ReadBytes(UnsafeUtility.AddressOf(ref transform), UnsafeUtility.SizeOf<TransformQvvs>());
        return transform;
    }

    public unsafe static float ReadFloat<T>(this ref T reader) where T : struct, BinaryReader
    {
        float value = default;
        reader.ReadBytes(&value, sizeof(float));
        return value;
    }
}

