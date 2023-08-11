using Unity.Collections;
using Unity.Entities;

namespace Latios.Calligraphics
{
    public struct GlyphMapper
    {
        [NativeDisableParallelForRestriction]
        private DynamicBuffer<LineToCharacterMap> _lineStarts;
        [NativeDisableParallelForRestriction]
        private DynamicBuffer<WordToCharacterMap> _wordStarts;
        [NativeDisableParallelForRestriction]
        private DynamicBuffer<CharacterToRenderGlyphMap> _characterToRenderGlyphMaps;
        
        public int characterCount => _characterToRenderGlyphMaps.Length;
        public int wordCount => _wordStarts.Length;
        public int lineCount => _lineStarts.Length;
        
        internal static GlyphMapper Create(ref DynamicBuffer<CharacterToRenderGlyphMap> characterToRenderGlyphMaps, ref DynamicBuffer<WordToCharacterMap> wordStarts, ref DynamicBuffer<LineToCharacterMap> lineStarts)
        {
            return new GlyphMapper
            {
                _lineStarts = lineStarts,
                _wordStarts = wordStarts,
                _characterToRenderGlyphMaps = characterToRenderGlyphMaps
            };
        }

        
        public int GetMappedGlyphIndex(int index, IndexType indexType)
        {
            switch (indexType)
            {
                case IndexType.Character:
                    return _characterToRenderGlyphMaps.Length > index ? _characterToRenderGlyphMaps[index].glyphIndex : -1;
                case IndexType.Word:
                    return _wordStarts.Length > index ? _characterToRenderGlyphMaps[_wordStarts[index].charIndex].glyphIndex : -1;
                case IndexType.Line:
                    return _lineStarts.Length > index ? _characterToRenderGlyphMaps[_lineStarts[index].charIndex].glyphIndex : -1;
            }

            return -1;
        }
        
        public int GetCharacterGlyphIndex(int characterIndex)
        {
            return _characterToRenderGlyphMaps.Length > characterIndex ? _characterToRenderGlyphMaps[characterIndex].glyphIndex : -1;
        }

        public int GetWordGlyphIndex(int wordIndex)
        {
            return _wordStarts.Length > wordIndex ? _characterToRenderGlyphMaps[_wordStarts[wordIndex].charIndex].glyphIndex : -1;
        }

        public int GetLineGlyphIndex(int lineIndex)
        {
            return _lineStarts.Length > lineIndex ? _characterToRenderGlyphMaps[_lineStarts[lineIndex].charIndex].glyphIndex : -1;
        }
        
        public int GetGlyphWordIndex(int glyphIndex)
        {
            if (_characterToRenderGlyphMaps[_wordStarts[^1].charIndex].glyphIndex <= glyphIndex)
            {
                return _wordStarts.Length - 1;
            }
            
            for (int i = 0; i < _wordStarts.Length - 1; i++)
            {
                if (_characterToRenderGlyphMaps[_wordStarts[i].charIndex].glyphIndex <= glyphIndex && _characterToRenderGlyphMaps[_wordStarts[i + 1].charIndex].glyphIndex > glyphIndex)
                {
                    return i;
                }
            }

            return -1;
        }
        
        public int GetGlyphLineIndex(int glyphIndex)
        {
            if (_characterToRenderGlyphMaps[_lineStarts[^1].charIndex].glyphIndex <= glyphIndex)
            {
                return _lineStarts.Length - 1;
            }

            for (int i = 0; i < _lineStarts.Length - 1; i++)
            {
                if (_characterToRenderGlyphMaps[_lineStarts[i].charIndex].glyphIndex <= glyphIndex && _characterToRenderGlyphMaps[_lineStarts[i + 1].charIndex].glyphIndex > glyphIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        public int GetWordLineIndex(int wordIndex)
        {
            var wordStartCharIndex = _wordStarts[wordIndex].charIndex;
            
            if (_lineStarts[^1].charIndex <= wordStartCharIndex)
            {
                return _lineStarts.Length - 1;
            }

            for (int i = 0; i < _lineStarts.Length - 1; i++)
            {
                if (_lineStarts[i].charIndex <= wordStartCharIndex && _lineStarts[i + 1].charIndex > wordStartCharIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        public enum IndexType
        {
            Character,
            Word,
            Line
        }
    }
}