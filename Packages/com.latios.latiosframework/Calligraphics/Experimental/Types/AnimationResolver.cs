using Latios.Kinemation.TextBackend;
using Unity.Entities;

namespace Latios.Calligraphics
{
    internal static class AnimationResolver
    {
        internal static void Initialize(ref TextAnimationTransition transition, ref Rng.RngSequence rng, GlyphMapper glyphMapper)
        {
            switch (transition.transitionValue)
            {
                case TransitionValue.Color:
                {
                    new ColorTransitionProvider().Initialize(ref transition, ref rng, glyphMapper);;
                    break;
                }
                case TransitionValue.Scale:
                {
                    new ScaleTransitionProvider().Initialize(ref transition, ref rng, glyphMapper);;
                    break;
                }
                case TransitionValue.Position:
                {
                    new PositionTransitionProvider().Initialize(ref transition, ref rng, glyphMapper);;
                    break;
                }
                case TransitionValue.NoisePosition:
                {
                    new NoisePositionTransitionProvider().Initialize(ref transition, ref rng, glyphMapper);;
                    break;
                }
                case TransitionValue.NoiseRotation:
                {
                    new NoiseRotationTransitionProvider().Initialize(ref transition, ref rng, glyphMapper);;
                    break;
                }
                default:
                {
                    new OpacityTransitionProvider().Initialize(ref transition, ref rng, glyphMapper);;
                    break;
                }
            }
        }

        internal static void SetValue(ref DynamicBuffer<RenderGlyph> renderGlyphs, TextAnimationTransition transition, GlyphMapper glyphMapper,
            int startIndex, int endIndex, float normalizedTime)
        {
            switch (transition.transitionValue)
            {
                case TransitionValue.Color:
                {
                    new ColorTransitionProvider().SetValue(ref renderGlyphs, transition, glyphMapper, startIndex, endIndex, normalizedTime);
                    break;
                }
                case TransitionValue.Scale:
                {
                    new ScaleTransitionProvider().SetValue(ref renderGlyphs, transition, glyphMapper, startIndex, endIndex, normalizedTime);
                    break;
                }
                case TransitionValue.Position:
                {
                    new PositionTransitionProvider().SetValue(ref renderGlyphs, transition, glyphMapper, startIndex, endIndex, normalizedTime);
                    break;
                }
                case TransitionValue.NoisePosition:
                {
                    new NoisePositionTransitionProvider().SetValue(ref renderGlyphs, transition, glyphMapper, startIndex, endIndex, normalizedTime);
                    break;
                }
                case TransitionValue.NoiseRotation:
                {
                    new NoiseRotationTransitionProvider().SetValue(ref renderGlyphs, transition, glyphMapper, startIndex, endIndex, normalizedTime);
                    break;
                }
                default:
                {
                    new OpacityTransitionProvider().SetValue(ref renderGlyphs, transition, glyphMapper, startIndex, endIndex, normalizedTime);
                    break;
                }
            }
        }

        internal static void  DisposeTransition(ref TextAnimationTransition transition)
        {
            switch (transition.transitionValue)
            {
                case TransitionValue.Color:
                {
                    ((ITransitionProvider)new ColorTransitionProvider()).DisposeTransition(ref transition);
                    break;
                }
                case TransitionValue.Scale:
                {
                    ((ITransitionProvider)new ScaleTransitionProvider()).DisposeTransition(ref transition);
                    break;
                }
                case TransitionValue.Position:
                {
                    ((ITransitionProvider)new PositionTransitionProvider()).DisposeTransition(ref transition);
                    break;
                }
                case TransitionValue.NoisePosition:
                {
                    ((ITransitionProvider)new NoisePositionTransitionProvider()).DisposeTransition(ref transition);
                    break;
                }
                case TransitionValue.NoiseRotation:
                {
                    ((ITransitionProvider)new NoiseRotationTransitionProvider()).DisposeTransition(ref transition);
                    break;
                }
                default:
                {
                    ((ITransitionProvider)new OpacityTransitionProvider()).DisposeTransition(ref transition);
                    break;
                }
            }
        }
    }
}