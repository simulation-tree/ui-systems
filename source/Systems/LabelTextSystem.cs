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
        private static readonly long charTypeHash;

        static LabelTextSystem()
        {
            charTypeHash = "System.Char".GetLongHashCode();
        }

        private readonly Text result;
        private readonly List<TryProcessLabel> processors;
        private readonly Dictionary<long, uint> tokenEntities;

        public LabelTextSystem()
        {
            result = new();
            processors = new();
            tokenEntities = new();
        }

        public readonly void Dispose()
        {
            tokenEntities.Dispose();
            processors.Dispose();
            result.Dispose();
        }

        readonly void ISystem.Start(in SystemContext context, in World world)
        {
        }

        readonly void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            Schema schema = world.Schema;
            int textRendererType = schema.GetComponentType<IsTextRenderer>();
            int processorType = schema.GetComponentType<IsLabelProcessor>();
            int labelTag = schema.GetTagType<IsLabel>();
            int characterArrayType = schema.GetArrayType<LabelCharacter>();
            int tokenComponentType = schema.GetComponentType<IsToken>();

            ReadOnlySpan<Chunk> chunks = world.Chunks;
            processors.Clear();
            tokenEntities.Clear();
            foreach (Chunk chunk in chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(processorType))
                {
                    ComponentEnumerator<IsLabelProcessor> components = chunk.GetComponents<IsLabelProcessor>(processorType);
                    for (int i = 0; i < components.length; i++)
                    {
                        ref IsLabelProcessor processor = ref components[i];
                        processors.Add(processor.function);
                    }
                }

                if (definition.ContainsComponent(tokenComponentType))
                {
                    ComponentEnumerator<IsToken> components = chunk.GetComponents<IsToken>(tokenComponentType);
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsToken token = ref components[i];
                        tokenEntities.TryAdd(token.hash, entities[i]);
                    }
                }
            }

            Span<TryProcessLabel> processorsSpan = processors.AsSpan();
            foreach (Chunk chunk in chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsTag(labelTag) && definition.ContainsComponent(textRendererType) && definition.ContainsArray(characterArrayType) && !definition.ContainsTag(Schema.DisabledTagType))
                {
                    ReadOnlySpan<uint> labelEntities = chunk.Entities;
                    ComponentEnumerator<IsTextRenderer> components = chunk.GetComponents<IsTextRenderer>(textRendererType);
                    for (int i = 0; i < labelEntities.Length; i++)
                    {
                        ref IsTextRenderer textRenderer = ref components[i];
                        ref rint textMeshReference = ref textRenderer.textMeshReference;
                        if (textMeshReference != default)
                        {
                            ProcessLabel(world, processorsSpan, textMeshReference, labelEntities[i]);
                        }
                    }
                }
            }
        }

        private readonly void ReplaceTokens(World world, Text text)
        {
            Span<char> span = text.AsSpan();
            if (span.Contains('{'))
            {
                Schema schema = world.Schema;
                schema.TryGetArrayType("System.Char", out int charArrayType);
                schema.TryGetArrayType("UI.Components.LabelCharacter", out int labelArrayType);
                schema.TryGetArrayType("Rendering.Components.TextCharacter", out int textArrayType);
                int position = 0;
                int start = 0;
                while (position < span.Length - 1)
                {
                    char c = span[position];
                    if (c == '{')
                    {
                        if (span[position + 1] == '{')
                        {
                            start = position;
                        }
                    }
                    else if (c == '}')
                    {
                        if (span[position + 1] == '}')
                        {
                            int length = position - start + 2;
                            ReadOnlySpan<char> token = span.Slice(start + 2, length - 4);
                            int separator = token.IndexOf(':');
                            if (separator != -1)
                            {
                                ReadOnlySpan<char> source = token.Slice(0, separator);
                                if (!uint.TryParse(source, out uint entity))
                                {
                                    if (!tokenEntities.TryGetValue(source.GetLongHashCode(), out entity))
                                    {
                                        position++;
                                        continue;
                                    }
                                }

                                ReadOnlySpan<char> type = token.Slice(separator + 1);
                                if (type.Length > 0)
                                {
                                    if (type.EndsWith("[]"))
                                    {
                                        type = type.Slice(0, type.Length - 2);
                                        if (schema.TryGetArrayType(type, out int arrayType))
                                        {
                                            text.Remove(start, length);
                                            if (arrayType == charArrayType || arrayType == labelArrayType || arrayType == textArrayType)
                                            {
                                                text.Insert(start, world.GetArray(entity, arrayType).AsSpan<char>());
                                            }
                                            else
                                            {
                                                //todo: handling other array types here?
                                                text.Insert(start, entity);
                                            }

                                            span = text.AsSpan();
                                            position = 0;
                                            start = 0;
                                            continue;
                                        }
                                        else
                                        {
                                            position++;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        if (schema.TryGetComponentType(type, out int componentType))
                                        {

                                        }
                                        else
                                        {
                                            position++;
                                            continue;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (!uint.TryParse(token, out uint entity))
                                {
                                    if (!tokenEntities.TryGetValue(token.GetLongHashCode(), out entity))
                                    {
                                        position++;
                                        continue;
                                    }
                                }

                                //replace part of text with entity
                                text.Remove(start, length);
                                text.Insert(start, entity);
                                span = text.AsSpan();
                                position = 0;
                                start = 0;
                                continue;
                            }
                        }
                    }

                    position++;
                }
            }
        }

        private readonly void ProcessLabel(World world, Span<TryProcessLabel> processors, rint textMeshReference, uint labelEntity)
        {
            Span<char> originalText = world.GetArray<LabelCharacter>(labelEntity).AsSpan<char>();
            result.CopyFrom(originalText);
            ReplaceTokens(world, result);

            //process the input text
            foreach (TryProcessLabel token in processors)
            {
                token.Invoke(result.AsSpan(), result);
            }

            uint textMeshEntity = world.GetReference(labelEntity, textMeshReference);
            Values<TextCharacter> targetText = world.GetArray<TextCharacter>(textMeshEntity);
            bool textIsDifferent;
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

        readonly void ISystem.Finish(in SystemContext context, in World world)
        {
        }
    }
}