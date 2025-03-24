using Collections.Generic;
using Rendering.Components;
using Simulation;
using System;
using UI.Components;
using UI.Functions;
using Unmanaged;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct LabelTextSystem : ISystem
    {
        private readonly Text result;
        private readonly List<TryProcessLabel> processors;

        public LabelTextSystem()
        {
            result = new();
            processors = new();
        }

        public readonly void Dispose()
        {
            processors.Dispose();
            result.Dispose();
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            int textRendererType = world.Schema.GetComponentType<IsTextRenderer>();
            int processorType = world.Schema.GetComponentType<IsLabelProcessor>();
            int labelTag = world.Schema.GetTagType<IsLabel>();
            int characterArrayType = world.Schema.GetArrayType<LabelCharacter>();

            processors.Clear();
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(processorType))
                {
                    ComponentEnumerator<IsLabelProcessor> components = chunk.GetComponents<IsLabelProcessor>(processorType);
                    for (int i = 0; i < components.length; i++)
                    {
                        ref IsLabelProcessor processor = ref components[i];
                        processors.Add(processor.function);
                    }
                }
            }

            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsTag(labelTag) && definition.ContainsComponent(textRendererType) && definition.ContainsArray(characterArrayType) && !definition.ContainsTag(Schema.DisabledTagType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsTextRenderer> components = chunk.GetComponents<IsTextRenderer>(textRendererType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsTextRenderer textRenderer = ref components[i];
                        ref rint textMeshReference = ref textRenderer.textMeshReference;
                        if (textMeshReference != default)
                        {
                            uint entity = entities[i];
                            Span<char> originalText = world.GetArray<LabelCharacter>(entity).AsSpan<char>();
                            result.CopyFrom(originalText);
                            foreach (TryProcessLabel token in processors)
                            {
                                token.Invoke(result.AsSpan(), result);
                            }

                            uint textMeshEntity = world.GetReference(entity, textMeshReference);
                            Values<TextCharacter> targetText = world.GetArray<TextCharacter>(textMeshEntity);
                            bool textIsDifferent = false;
                            if (targetText.Length != result.Length)
                            {
                                //make sure destination array matches length
                                targetText.Resize(result.Length);
                                textIsDifferent = true;
                            }
                            else
                            {
                                textIsDifferent = !targetText.AsSpan<char>().SequenceEqual(result.AsSpan());
                            }

                            if (textIsDifferent)
                            {
                                ref IsTextMeshRequest request = ref world.GetComponent<IsTextMeshRequest>(textMeshEntity);
                                request.loaded = false;
                                result.CopyTo(targetText.AsSpan<char>());
                            }
                        }
                    }
                }
            }
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
        }
    }
}