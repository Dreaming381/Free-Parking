using Unity.Collections;
using Unity.Entities;

namespace Latios.Calligraphics
{
    internal struct GlyphMappingWriter
    {
        [NativeDisableParallelForRestriction]
        private DynamicBuffer<LineToCharacterMap> m_lineStarts;
        [NativeDisableParallelForRestriction]
        private DynamicBuffer<WordToCharacterMap> m_wordStarts;
        [NativeDisableParallelForRestriction]
        private DynamicBuffer<CharacterToRenderGlyphMap> m_characterToRenderGlyphMaps;
        
        internal static GlyphMappingWriter Create(ref DynamicBuffer<CharacterToRenderGlyphMap> characterToRenderGlyphMaps, ref DynamicBuffer<WordToCharacterMap> wordStarts, ref DynamicBuffer<LineToCharacterMap> lineStarts)
        {
            return new GlyphMappingWriter
            {
                m_lineStarts = lineStarts,
                m_wordStarts = wordStarts,
                m_characterToRenderGlyphMaps = characterToRenderGlyphMaps
            };
        }

        internal void AddLineStart(int charIndex)
        {
            m_lineStarts.Add(new LineToCharacterMap {charIndex = charIndex});
        }
        
        internal void AddWordStart(int charIndex)
        {
            m_wordStarts.Add(new WordToCharacterMap {charIndex = charIndex});
        }
        
        internal void AddCharacterToRenderGlyphMap(int glyphIndex)
        {
            m_characterToRenderGlyphMaps.Add(new CharacterToRenderGlyphMap { glyphIndex = glyphIndex });
        }
        
        internal void UpdateCharacterToRenderGlyphMap(int index, int glyphIndex)
        {
            m_characterToRenderGlyphMaps[index] = new CharacterToRenderGlyphMap { glyphIndex = glyphIndex };
        }
        
        internal void Clear()
        {
            m_lineStarts.Clear();
            m_wordStarts.Clear();
            m_characterToRenderGlyphMaps.Clear();
        }
    }
}