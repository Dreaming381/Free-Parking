using System;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Latios.Calligraphics
{
    
    public enum TextScope : byte
    {
        Glyph,
        Word,
        Line,
        All
    }
    
    public enum TransitionValue : byte
    {
        Opacity,
        Scale,
        Color,
        NoisePosition,
        NoiseRotation,
        Position,
        Rotation,
    }

    [Flags]
    public enum TransitionEndBehavior : byte
    {
        Revert = 1,
        KeepFinalValue = 2,
        Loop = 4
    }

    [ChunkSerializable]
    [InternalBufferCapacity(0)]
    [StructLayout(LayoutKind.Explicit)]
    public struct TextAnimationTransition : IBufferElementData
    {
        [FieldOffset(0)] public float currentTime;
        [FieldOffset(4)] public float transitionDuration;
        [FieldOffset(8)] public float transitionTimeOffset;
        [FieldOffset(12)] public float loopDelay;
        [FieldOffset(16)] public int loopCount;
        [FieldOffset(20)] public int currentLoop;
        [FieldOffset(24)] public int startIndex;
        [FieldOffset(28)] public int endIndex;
        [FieldOffset(32)] public TextScope scope;
        [FieldOffset(33)] public InterpolationType interpolation;
        [FieldOffset(34)] public TransitionValue transitionValue;
        [FieldOffset(35)] public TransitionEndBehavior endBehavior;
        
        //Opacity
        [FieldOffset(36)] public byte startValueByte;
        [FieldOffset(37)] public byte endValueByte;
        
        //Scale, Position
        [FieldOffset(36)] public float2 startValueFloat2;
        [FieldOffset(44)] public float2 endValueFloat2;
        
        //Rotation
        [FieldOffset(36)] public float startValueFloat;
        [FieldOffset(40)] public float endValueFloat;
        
        //Color
        [FieldOffset(36)]public Color32 startValueBlColor;
        [FieldOffset(44)]public Color32 endValueBlColor;
        [FieldOffset(52)]public Color32 startValueTrColor;
        [FieldOffset(60)]public Color32 endValueTrColor;
        [FieldOffset(68)]public Color32 startValueBrColor;
        [FieldOffset(76)]public Color32 endValueBrColor;
        [FieldOffset(84)]public Color32 startValueTlColor;
        [FieldOffset(92)]public Color32 endValueTlColor;

        [FieldOffset(36)] public float3 startValueFloat3;
        [FieldOffset(48)] public float3 endValueFloat3;
        
        //Randomized Noise Values
        [FieldOffset(36)] public IntPtr startValuesBuffer;
        [FieldOffset(44)] public IntPtr endValuesBuffer;
        [FieldOffset(52)] public int valuesBufferLength;
        
        //PositionNoise
        [FieldOffset(56)] public float2 noiseScaleFloat2;
        
        //RotationNoise
        [FieldOffset(56)] public float noiseScaleFloat;
    }
}