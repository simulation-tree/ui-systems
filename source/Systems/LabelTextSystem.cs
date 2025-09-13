using Collections.Generic;
using Rendering.Components;
using Simulation;
using System;
using UI.Components;
using UI.Functions;
using UI.Messages;
using Unmanaged;
using Worlds;

namespace UI.Systems
{
    public partial class LabelTextSystem : SystemBase, IListener<UIUpdate>
    {
        private readonly World world;
        private readonly Dictionary<uint, Text> results;
        private readonly List<TryProcessLabel> processors;
        private readonly Dictionary<long, uint> tokenEntities;
        private readonly int textRendererType;
        private readonly int processorType;
        private readonly int labelTag;
        private readonly int labelCharacterArrayType;
        private readonly int textCharacterArrayType;
        private readonly int tokenType;
        private readonly int requestType;
        private readonly int charArrayType;

        public LabelTextSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            results = new(4);
            processors = new(4);
            tokenEntities = new(4);

            Schema schema = world.Schema;
            textRendererType = schema.GetComponentType<IsTextRenderer>();
            processorType = schema.GetComponentType<IsLabelProcessor>();
            labelTag = schema.GetTagType<IsLabel>();
            labelCharacterArrayType = schema.GetArrayType<LabelCharacter>();
            textCharacterArrayType = schema.GetArrayType<TextCharacter>();
            tokenType = schema.GetComponentType<IsToken>();
            requestType = schema.GetComponentType<IsTextMeshRequest>();
            schema.TryGetArrayType("System.Char", out charArrayType);
        }

        public override void Dispose()
        {
            foreach (Text result in results.Values)
            {
                result.Dispose();
            }

            tokenEntities.Dispose();
            processors.Dispose();
            results.Dispose();
        }

        void IListener<UIUpdate>.Receive(ref UIUpdate message)
        {
            processors.Clear();
            tokenEntities.Clear();
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.componentTypes.Contains(processorType))
                {
                    ComponentEnumerator<IsLabelProcessor> components = chunk.GetComponents<IsLabelProcessor>(processorType);
                    for (int i = 0; i < components.length; i++)
                    {
                        ref IsLabelProcessor processor = ref components[i];
                        processors.Add(processor.function);
                    }
                }

                if (chunk.componentTypes.Contains(tokenType))
                {
                    ComponentEnumerator<IsToken> components = chunk.GetComponents<IsToken>(tokenType);
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsToken token = ref components[i];
                        tokenEntities.TryAdd(token.hash, entities[i]);
                    }
                }
            }

            Span<TryProcessLabel> processorsSpan = processors.AsSpan();
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.tagTypes.Contains(labelTag) && chunk.componentTypes.Contains(textRendererType) && chunk.ArrayTypes.Contains(labelCharacterArrayType) && !chunk.IsDisabled)
                {
                    ReadOnlySpan<uint> labelEntities = chunk.Entities;
                    ComponentEnumerator<IsTextRenderer> components = chunk.GetComponents<IsTextRenderer>(textRendererType);
                    for (int i = 0; i < labelEntities.Length; i++)
                    {
                        ref IsTextRenderer textRenderer = ref components[i];
                        ref rint textMeshReference = ref textRenderer.textMeshReference;
                        if (textMeshReference != default)
                        {
                            ProcessLabel(processorsSpan, textMeshReference, labelEntities[i]);
                        }
                    }
                }
            }
        }

        private void ReplaceTokens(Text text)
        {
            Span<char> span = text.AsSpan();
            if (span.Contains('{'))
            {
                Schema schema = world.Schema;
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
                                            if (arrayType == charArrayType || arrayType == labelCharacterArrayType || arrayType == textCharacterArrayType)
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

        private void ProcessLabel(Span<TryProcessLabel> processors, rint textMeshReference, uint labelEntity)
        {
            Span<char> originalText = world.GetArray<LabelCharacter>(labelEntity, labelCharacterArrayType).AsSpan<char>();
            ref Text result = ref results.TryGetValue(labelEntity, out bool contains);
            if (!contains)
            {
                result = ref results.Add(labelEntity);
                result = new(0);
            }

            result.CopyFrom(originalText);
            ReplaceTokens(result);

            //process the input text
            foreach (TryProcessLabel token in processors)
            {
                token.Invoke(result.AsSpan(), result);
            }

            uint textMeshEntity = world.GetReference(labelEntity, textMeshReference);
            Values<TextCharacter> targetText = world.GetArray<TextCharacter>(textMeshEntity, textCharacterArrayType);
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
                ref IsTextMeshRequest request = ref world.GetComponent<IsTextMeshRequest>(textMeshEntity, requestType);
                request.loaded = false;
                result.CopyTo(targetText.AsSpan<char>());
            }
        }
    }
}