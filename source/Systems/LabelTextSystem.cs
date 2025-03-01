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

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                processors.Dispose();
                result.Dispose();
            }
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                systemContainer.Write(new LabelTextSystem());
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            ComponentType textRendererType = world.Schema.GetComponentType<IsTextRenderer>();
            ComponentType processorType = world.Schema.GetComponentType<IsLabelProcessor>();
            TagType labelTag = world.Schema.GetTagType<IsLabel>();
            ArrayElementType characterArrayType = world.Schema.GetArrayType<LabelCharacter>();

            processors.Clear();
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(processorType))
                {
                    USpan<IsLabelProcessor> components = chunk.GetComponents<IsLabelProcessor>(processorType);
                    for (uint i = 0; i < components.Length; i++)
                    {
                        ref IsLabelProcessor processor = ref components[i];
                        processors.Add(processor.function);
                    }
                }
            }

            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsTag(labelTag) && definition.ContainsComponent(textRendererType) && definition.ContainsArray(characterArrayType) && !definition.ContainsTag(TagType.Disabled))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<IsTextRenderer> components = chunk.GetComponents<IsTextRenderer>(textRendererType);
                    for (uint i = 0; i < entities.Length; i++)
                    {
                        ref IsTextRenderer textRenderer = ref components[i];
                        ref rint textMeshReference = ref textRenderer.textMeshReference;
                        if (textMeshReference != default)
                        {
                            uint entity = entities[i];
                            USpan<char> originalText = world.GetArray<LabelCharacter>(entity).AsSpan().As<char>();
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
                                targetText.Length = result.Length;
                                textIsDifferent = true;
                            }
                            else
                            {
                                textIsDifferent = !targetText.AsSpan().As<char>().SequenceEqual(result.AsSpan());
                            }

                            if (textIsDifferent)
                            {
                                ref IsTextMeshRequest request = ref world.GetComponent<IsTextMeshRequest>(textMeshEntity);
                                request.loaded = false;
                                result.CopyTo(targetText.AsSpan().As<char>());
                            }
                        }
                    }
                }
            }
        }
    }
}