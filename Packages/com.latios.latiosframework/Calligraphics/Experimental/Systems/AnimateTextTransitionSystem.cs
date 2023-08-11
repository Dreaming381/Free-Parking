using Latios.Calligraphics.RichText;
using Latios.Calligraphics.RichText.Parsing;
using Latios.Kinemation.Systems;
using Latios.Kinemation.TextBackend;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

using static Unity.Entities.SystemAPI;

namespace Latios.Calligraphics.Systems
{
    [UpdateInGroup(typeof(UpdatePresentationSystemGroup))]
    [UpdateAfter(typeof(GenerateGlyphsSystem))]
    [UpdateBefore(typeof(KinemationRenderUpdateSuperSystem))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoCreation]
    public partial struct AnimateTextTransitionSystem : ISystem
    {
        EntityQuery m_query;
        Rng         m_rng;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_query = state.Fluent()
                      .WithAll<TextAnimationTransition>(  false)
                      .WithAll<RenderGlyph>(              false)
                      .WithAll<TextRenderControl>(        false)
                      .WithAll<CharacterToRenderGlyphMap>(true)
                      .WithAll<WordToCharacterMap>(       true)
                      .WithAll<LineToCharacterMap>(       true)
                      .Build();

            m_rng = new Rng("AnimateTextTransitionSystem");
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new TransitionJob
            {
                rng                     = m_rng,
                deltaTime               = state.WorldUnmanaged.Time.DeltaTime,
                transitionHandle        = GetBufferTypeHandle<TextAnimationTransition>(false),
                textRenderControlHandle = GetComponentTypeHandle<TextRenderControl>(false),
                renderGlyphHandle       = GetBufferTypeHandle<RenderGlyph>(false),
                characterMapHandle      = GetBufferTypeHandle<CharacterToRenderGlyphMap>(true),
                wordStartHandle         = GetBufferTypeHandle<WordToCharacterMap>(true),
                lineStartHandle         = GetBufferTypeHandle<LineToCharacterMap>(true),
            }.ScheduleParallel(m_query, state.Dependency);

            m_rng.Shuffle();
        }

        public void OnDestroy(ref SystemState state)
        {
            state.Dependency = new DisposeJob
            {
                transitionHandle = GetBufferTypeHandle<TextAnimationTransition>(false),
            }.ScheduleParallel(m_query, state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public partial struct TransitionJob : IJobChunk
        {
            public float deltaTime;
            public Rng   rng;

            public BufferTypeHandle<TextAnimationTransition> transitionHandle;
            public BufferTypeHandle<RenderGlyph>             renderGlyphHandle;
            [ReadOnly]
            public BufferTypeHandle<CharacterToRenderGlyphMap> characterMapHandle;
            [ReadOnly]
            public BufferTypeHandle<WordToCharacterMap> wordStartHandle;
            [ReadOnly]
            public BufferTypeHandle<LineToCharacterMap>   lineStartHandle;
            public ComponentTypeHandle<TextRenderControl> textRenderControlHandle;

            private GlyphMapper m_glyphMapper;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var random              = rng.GetSequence(unfilteredChunkIndex);
                var transitionBuffers   = chunk.GetBufferAccessor(ref transitionHandle);
                var renderGlyphBuffers  = chunk.GetBufferAccessor(ref renderGlyphHandle);
                var characterMapBuffers = chunk.GetBufferAccessor(ref characterMapHandle);
                var wordStartBuffers    = chunk.GetBufferAccessor(ref wordStartHandle);
                var lineStartBuffers    = chunk.GetBufferAccessor(ref lineStartHandle);
                var textRenderControls  = chunk.GetNativeArray(ref textRenderControlHandle);

                for (int indexInChunk = 0; indexInChunk < chunk.Count; indexInChunk++)
                {
                    var transitions       = transitionBuffers[indexInChunk];
                    var renderGlyphs      = renderGlyphBuffers[indexInChunk];
                    var characterMaps     = characterMapBuffers[indexInChunk];
                    var wordStarts        = wordStartBuffers[indexInChunk];
                    var lineStarts        = lineStartBuffers[indexInChunk];
                    var textRenderControl = textRenderControls[indexInChunk];

                    m_glyphMapper = GlyphMapper.Create(ref characterMaps, ref wordStarts, ref lineStarts);

                    for (int i = 0; i < transitions.Length; i++)
                    {
                        var transition = transitions[i];

                        //Loop if appropriate
                        if ((transition.endBehavior & TransitionEndBehavior.Loop) == TransitionEndBehavior.Loop &&
                            transition.currentTime >= transition.transitionTimeOffset + transition.transitionDuration &&
                            transition.currentTime >= transition.loopDelay)
                        {
                            transition.currentLoop++;
                            transition.currentTime = 0f;

                            if (transition.currentLoop > transition.loopCount && (transition.endBehavior & TransitionEndBehavior.Revert) == TransitionEndBehavior.Revert)
                            {
                                AnimationResolver.DisposeTransition(ref transition);
                                transitions.RemoveAtSwapBack(i);
                                i--;
                                continue;
                            }
                        }

                        if (transition.currentTime == 0)
                        {
                            AnimationResolver.Initialize(ref transition, ref random, m_glyphMapper);
                        }

                        //Get scope indices
                        var startIndex = 0;
                        var endIndex   = 0;
                        switch (transition.scope)
                        {
                            case TextScope.All:
                                startIndex = 0;
                                endIndex   = renderGlyphs.Length;
                                break;
                            case TextScope.Glyph:
                                startIndex = m_glyphMapper.GetCharacterGlyphIndex(transition.startIndex);
                                endIndex   = m_glyphMapper.GetCharacterGlyphIndex(transition.endIndex);
                                break;
                            case TextScope.Word:
                                startIndex = m_glyphMapper.GetWordGlyphIndex(transition.startIndex);
                                if (transition.endIndex >= m_glyphMapper.wordCount - 1)
                                {
                                    endIndex = renderGlyphs.Length - 1;
                                }
                                else if (transition.endIndex == transition.startIndex)
                                    endIndex = m_glyphMapper.GetWordGlyphIndex(transition.endIndex + 1) - 1;
                                else
                                    endIndex = m_glyphMapper.GetWordGlyphIndex(transition.endIndex);

                                break;
                            case TextScope.Line:
                                startIndex = m_glyphMapper.GetLineGlyphIndex(transition.startIndex);
                                if (transition.endIndex >= m_glyphMapper.lineCount - 1)
                                {
                                    endIndex = renderGlyphs.Length - 1;
                                }
                                else if (transition.endIndex == transition.startIndex)
                                    endIndex = m_glyphMapper.GetLineGlyphIndex(transition.endIndex + 1) - 1;
                                else
                                    endIndex = m_glyphMapper.GetLineGlyphIndex(transition.endIndex);

                                break;
                        }

                        if (startIndex > -1 && endIndex >= startIndex)
                        {
                            //Apply transition
                            float t = (transition.currentTime - transition.transitionTimeOffset) /
                                      transition.transitionDuration;

                            AnimationResolver.SetValue(ref renderGlyphs, transition, m_glyphMapper, startIndex,
                                                       endIndex, t);
                        }

                        transition.currentTime += deltaTime;
                        transitions[i]          = transition;
                    }

                    textRenderControl.flags          = TextRenderControl.Flags.Dirty;
                    textRenderControls[indexInChunk] = textRenderControl;
                }
            }
        }

        //[BurstCompile]
        public struct DisposeJob : IJobChunk
        {
            public BufferTypeHandle<TextAnimationTransition> transitionHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var transitionBuffers = chunk.GetBufferAccessor(ref transitionHandle);

                for (int indexInChunk = 0; indexInChunk < chunk.Count; indexInChunk++)
                {
                    var transitions = transitionBuffers[indexInChunk];
                    for (int i = 0; i < transitions.Length; i++)
                    {
                        var transition = transitions[i];
                        AnimationResolver.DisposeTransition(ref transition);
                    }
                }
            }
        }
    }
}

