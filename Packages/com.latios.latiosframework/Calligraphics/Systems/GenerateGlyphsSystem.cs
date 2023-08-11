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
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(UpdatePresentationSystemGroup))]
    [UpdateBefore(typeof(KinemationRenderUpdateSuperSystem))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoCreation]
    public partial struct GenerateGlyphsSystem : ISystem
    {
        EntityQuery m_singleFontQuery;
        EntityQuery m_multiFontQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_singleFontQuery = state.Fluent()
                                .WithAll<FontBlobReference>(    true)
                                .WithAll<RenderGlyph>(          false)
                                .WithAll<CharacterToRenderGlyphMap>(          false)
                                .WithAll<WordToCharacterMap>(          false)
                                .WithAll<LineToCharacterMap>(          false)
                                .WithAll<CalliByte>(            true)
                                .WithAll<TextBaseConfiguration>(true)
                                .WithAll<TextRenderControl>(    false)
                                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new Job
            {
                calliByteHandle             = GetBufferTypeHandle<CalliByte>(true),
                textRenderControlHandle     = GetComponentTypeHandle<TextRenderControl>(false),
                renderGlyphHandle           = GetBufferTypeHandle<RenderGlyph>(false),
                characterMapHandle          = GetBufferTypeHandle<CharacterToRenderGlyphMap>(false),
                wordStartHandle             = GetBufferTypeHandle<WordToCharacterMap>(false),
                lineStartHandle             = GetBufferTypeHandle<LineToCharacterMap>(false),
                textBaseConfigurationHandle = GetComponentTypeHandle<TextBaseConfiguration>(true),
                fontBlobReferenceHandle     = GetComponentTypeHandle<FontBlobReference>(true),
            }.ScheduleParallel(m_singleFontQuery, state.Dependency);
        }

        [BurstCompile]
        public partial struct Job : IJobChunk
        {
            public BufferTypeHandle<RenderGlyph>          renderGlyphHandle;
            public BufferTypeHandle<CharacterToRenderGlyphMap>            characterMapHandle;
            public BufferTypeHandle<WordToCharacterMap>            wordStartHandle;
            public BufferTypeHandle<LineToCharacterMap>            lineStartHandle;
            public ComponentTypeHandle<TextRenderControl> textRenderControlHandle;

            [ReadOnly]
            public BufferTypeHandle<CalliByte> calliByteHandle;
            [ReadOnly]
            public ComponentTypeHandle<TextBaseConfiguration> textBaseConfigurationHandle;
            [ReadOnly]
            public ComponentTypeHandle<FontBlobReference> fontBlobReferenceHandle;

            [NativeDisableContainerSafetyRestriction]
            private NativeList<RichTextTag> m_richTextTags;
            
            [NativeDisableParallelForRestriction]
            private GlyphMappingWriter m_glyphMappingWriter;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var calliBytesBuffers      = chunk.GetBufferAccessor(ref calliByteHandle);
                var renderGlyphBuffers     = chunk.GetBufferAccessor(ref renderGlyphHandle);
                var characterMapBuffers    = chunk.GetBufferAccessor(ref characterMapHandle);
                var wordStartBuffers       = chunk.GetBufferAccessor(ref wordStartHandle);
                var lineStartBuffers       = chunk.GetBufferAccessor(ref lineStartHandle);
                var textBaseConfigurations = chunk.GetNativeArray(ref textBaseConfigurationHandle);
                var fontBlobReferences     = chunk.GetNativeArray(ref fontBlobReferenceHandle);
                var textRenderControls     = chunk.GetNativeArray(ref textRenderControlHandle);

                for (int indexInChunk = 0; indexInChunk < chunk.Count; indexInChunk++)
                {
                    var calliBytes            = calliBytesBuffers[indexInChunk];
                    var renderGlyphs          = renderGlyphBuffers[indexInChunk];
                    var characterMaps            = characterMapBuffers[indexInChunk];
                    var lineStarts            = lineStartBuffers[indexInChunk];
                    var wordStarts            = wordStartBuffers[indexInChunk];
                    var fontBlobReference     = fontBlobReferences[indexInChunk];
                    var textBaseConfiguration = textBaseConfigurations[indexInChunk];
                    var textRenderControl     = textRenderControls[indexInChunk];
                    
                    m_glyphMappingWriter = GlyphMappingWriter.Create(ref characterMaps, ref wordStarts, ref lineStarts);

                    if (!m_richTextTags.IsCreated)
                    {
                        m_richTextTags = new NativeList<RichTextTag>(Allocator.Temp);
                    }

                    RichTextParser.ParseTags(ref m_richTextTags, calliBytes);

                    GlyphGeneration.CreateRenderGlyphs(ref renderGlyphs, ref m_glyphMappingWriter, ref fontBlobReference.blob.Value, in calliBytes, in textBaseConfiguration, ref m_richTextTags);

                    textRenderControl.flags          = TextRenderControl.Flags.Dirty;
                    textRenderControls[indexInChunk] = textRenderControl;
                }
            }
        }
    }
}

