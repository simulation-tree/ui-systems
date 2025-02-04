using Collections;
using UI.Components;
using UI.Functions;
using Rendering.Components;
using Simulation;
using System;
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
            processors.Clear();
            ComponentQuery<IsLabelProcessor> processorQuery = new(world);
            foreach (var r in processorQuery)
            {
                processors.Add(r.component1.function);
            }

            ComponentQuery<IsLabel, IsTextRenderer> labelQuery = new(world);
            labelQuery.ExcludeDisabled(true);
            labelQuery.RequireArray<LabelCharacter>();
            foreach (var r in labelQuery)
            {
                ref rint textMeshReference = ref r.component2.textMeshReference;
                if (textMeshReference != default)
                {
                    USpan<char> originalText = world.GetArray<LabelCharacter>(r.entity).As<char>();
                    result.CopyFrom(originalText);
                    foreach (TryProcessLabel token in processors)
                    {
                        token.Invoke(result.AsSpan(), result);
                    }

                    uint textMeshEntity = world.GetReference(r.entity, textMeshReference);
                    uint arrayLength = world.GetArrayLength<TextCharacter>(textMeshEntity);
                    bool lengthChanged = false;
                    if (arrayLength != result.Length)
                    {
                        //make sure destination array matches length
                        world.ResizeArray<TextCharacter>(textMeshEntity, result.Length);
                        lengthChanged = true;
                    }

                    USpan<char> targetText = world.GetArray<TextCharacter>(textMeshEntity).As<char>();
                    if (lengthChanged || !targetText.SequenceEqual(result.AsSpan()))
                    {
                        ref IsTextMeshRequest request = ref world.GetComponent<IsTextMeshRequest>(textMeshEntity);
                        request.loaded = false;
                        result.CopyTo(targetText);
                    }
                }
            }
        }
    }
}