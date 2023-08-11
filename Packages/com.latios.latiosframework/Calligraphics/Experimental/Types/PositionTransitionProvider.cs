using Latios.Kinemation.TextBackend;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Latios.Calligraphics
{
    public struct PositionTransitionProvider : ITransitionProvider
    {
        public void Initialize(ref TextAnimationTransition transition, ref Rng.RngSequence rng, GlyphMapper glyphMapper)
        {
            if (transition.currentLoop > 0 && (transition.endBehavior & TransitionEndBehavior.KeepFinalValue) == TransitionEndBehavior.KeepFinalValue)
            {
                (transition.startValueFloat2, transition.endValueFloat2) = (transition.endValueFloat2, transition.startValueFloat2);
            }
        }
        
        public void SetValue(ref DynamicBuffer<RenderGlyph> renderGlyphs, TextAnimationTransition transition, GlyphMapper glyphMapper, int startIndex, int endIndex, float normalizedTime)
        {
            var value = transition.startValueFloat2;
            
            if (transition.currentTime >= transition.transitionTimeOffset)
            {
                value = Interpolation.Interpolate(transition.startValueFloat2, transition.endValueFloat2, normalizedTime, transition.interpolation);
            }

            //Apply new values
            for (int i = startIndex; i <= math.min(endIndex, renderGlyphs.Length - 1); i++)
            {
                var renderGlyph = renderGlyphs[i];
                SetPositionValue(ref renderGlyph, value);
                renderGlyphs[i] = renderGlyph;
            }
        }
        
        private void SetPositionValue(ref RenderGlyph glyph, float2 value)
        {
            var blPosition       = glyph.blPosition + value;
            var trPosition       = glyph.trPosition + value;

            glyph.blPosition = blPosition;
            glyph.trPosition = trPosition;
        }
    }
}