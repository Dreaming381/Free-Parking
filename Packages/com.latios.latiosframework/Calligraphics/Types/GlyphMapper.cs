using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Latios.Calligraphics
{
    public struct GlyphMapper
    {
        NativeArray<int2> m_buffer;
        const int         kHeaderSize = 5;

        public int lineCount => m_buffer[0].y;
        public int wordCount => m_buffer[1].y;
        public int characterCountNoTags => m_buffer[2].y;
        public int characterCountWithTags => m_buffer[3].y;
        public int byteCountWithTags => m_buffer[4].y;

        public GlyphMapper(DynamicBuffer<GlyphMappingElement> mappingBuffer)
        {
            m_buffer = mappingBuffer.AsNativeArray().Reinterpret<int2>();
        }

        public int2 GetGlyphStartIndexAndCountForLine(int lineIndex) => m_buffer[m_buffer[0].x + lineIndex];

        public int2 GetGlyphStartIndexAndCountForWord(int wordIndex) => m_buffer[m_buffer[1].x + wordIndex];

        public bool TryGetGlyphIndexForCharNoTags(int charIndex, out int glyphIndex)
        {
            var batchIndex = charIndex / 32;
            var bitIndex   = charIndex % 32;
            var batch      = m_buffer[m_buffer[2].x + batchIndex];
            var mask       = 0xffffffff >> (32 - bitIndex);
            glyphIndex     = batch.x + math.select(math.countbits(mask & batch.y), 0, bitIndex == 0);
            return (batch.y & (1 << bitIndex)) != 0;
        }

        public bool TryGetGlyphIndexForCharWithTags(int charIndex, out int glyphIndex)
        {
            var batchIndex = charIndex / 32;
            var bitIndex   = charIndex % 32;
            var batch      = m_buffer[m_buffer[3].x + batchIndex];
            var mask       = 0xffffffff >> (32 - bitIndex);
            glyphIndex     = batch.x + math.select(math.countbits(mask & batch.y), 0, bitIndex == 0);
            return (batch.y & (1 << bitIndex)) != 0;
        }

        public bool TryGetGlyphIndexForByte(int byteIndex, out int glyphIndex)
        {
            var batchIndex = byteIndex / 32;
            var bitIndex   = byteIndex % 32;
            var batch      = m_buffer[m_buffer[4].x + batchIndex];
            var mask       = 0xffffffff >> (32 - bitIndex);
            glyphIndex     = batch.x + math.select(math.countbits(mask & batch.y), 0, bitIndex == 0);
            return (batch.y & (1 << bitIndex)) != 0;
        }
    }
}

