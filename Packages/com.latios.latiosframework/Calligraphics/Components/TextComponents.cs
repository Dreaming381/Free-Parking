using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Latios.Calligraphics
{
    public struct FontBlobReference : IComponentData
    {
        public BlobAssetReference<FontBlob> blob;
    }

    public struct TextBaseConfiguration : IComponentData
    {
        public float     fontSize;
        public float     maxLineWidth;
        public Color32   color;
        public AlignMode alignMode;
    }

    [InternalBufferCapacity(0)]
    public struct AdditionalFontMaterialEntity : IBufferElementData
    {
        public EntityWith<FontBlobReference> entity;
    }

    [InternalBufferCapacity(0)]
    public struct CalliByte : IBufferElementData
    {
        public byte element;
    }
    
    [InternalBufferCapacity(0)]
    public struct CharacterToRenderGlyphMap : IBufferElementData
    {
        public int glyphIndex;
    }

    [InternalBufferCapacity(0)]
    public struct LineToCharacterMap : IBufferElementData
    {
        public int charIndex;
    }

    [InternalBufferCapacity(0)]
    public struct WordToCharacterMap : IBufferElementData
    {
        public int charIndex;
    }

    

    public enum AlignMode : byte
    {
        Left = 0x0,
        Right = 0x1,
        Center = 0x2,
        Justified = 0x3,
        //
        Top = 0x0,
        Middle = 0x1 << 2,
        Bottom = 0x2 << 2,
        //
        HorizontalMask = 0x3,
        VerticalMask = 0xc,
    }
}

