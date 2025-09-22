using Collections.Generic;
using Rendering;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Transforms.Components;
using UI.Components;
using UI.Messages;
using Worlds;

namespace UI.Systems
{
    [SkipLocalsInit]
    public partial class ResizingSystem : SystemBase, IListener<UIUpdate>
    {
        private readonly World world;
        private readonly Dictionary<uint, bool> lastPressedPointers;
        private uint resizingEntity;
        private IsResizable.EdgeMask resizeBoundary;
        private Pointer activePointer;
        private Vector2 lastPointerPosition;
        private readonly int pointerType;
        private readonly int resizableType;
        private readonly int positionType;
        private readonly int scaleType;

        public ResizingSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            lastPressedPointers = new(4);
            pointerType = world.Schema.GetComponentType<IsPointer>();
            resizableType = world.Schema.GetComponentType<IsResizable>();
            positionType = world.Schema.GetComponentType<Position>();
            scaleType = world.Schema.GetComponentType<Scale>();
        }

        public override void Dispose()
        {
            lastPressedPointers.Dispose();
        }

        void IListener<UIUpdate>.Receive(ref UIUpdate message)
        {
            const int PointerCapacity = 16;
            Span<uint> pointerEntities = stackalloc uint[PointerCapacity];
            Span<IsPointer> pointerComponents = stackalloc IsPointer[PointerCapacity];
            int pointerEntityCount = 0;
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.ComponentTypes.Contains(pointerType) && !chunk.IsDisabled)
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsPointer> components = chunk.GetComponents<IsPointer>(pointerType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        uint entity = entities[i];
                        if (!lastPressedPointers.ContainsKey(entity))
                        {
                            ref IsPointer pointer = ref components[i];
                            lastPressedPointers.Add(entity, pointer.HasPrimaryIntent);
                        }
                    }

                    //todo: this will fail if there are more than capacity
                    entities.CopyTo(pointerEntities.Slice(pointerEntityCount));
                    components.CopyTo(pointerComponents.Slice(pointerEntityCount));
                    pointerEntityCount += entities.Length;
                }
            }

            if (resizingEntity == default)
            {
                ComponentQuery<IsResizable, Position, Scale> resizableQuery = new(world);
                resizableQuery.ExcludeDisabled(true);
                foreach (var r in resizableQuery)
                {
                    Resizable resizable = Entity.Get<Resizable>(world, r.entity);
                    LayerMask resizableMask = r.component1.selectionMask;
                    for (int p = 0; p < pointerEntityCount; p++)
                    {
                        ref IsPointer pointer = ref pointerComponents[p];
                        LayerMask pointerSelectionMask = pointer.selectionMask;
                        if (pointerSelectionMask.ContainsAll(resizableMask))
                        {
                            uint pointerEntity = pointerEntities[p];
                            if (pointer.HasPrimaryIntent && !lastPressedPointers[pointerEntity])
                            {
                                Vector2 pointerPosition = pointer.position;
                                IsResizable.EdgeMask boundary = resizable.GetBoundary(pointerPosition);
                                if (boundary != default)
                                {
                                    resizingEntity = r.entity;
                                    resizeBoundary = boundary;
                                    activePointer = new Entity(world, pointerEntity).As<Pointer>();
                                    lastPointerPosition = pointerPosition;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            for (int p = 0; p < pointerEntityCount; p++)
            {
                ref IsPointer pointer = ref pointerComponents[p];
                lastPressedPointers[pointerEntities[p]] = pointer.HasPrimaryIntent;
            }

            if (resizingEntity != default)
            {
                if (!activePointer.HasPrimaryIntent)
                {
                    resizingEntity = default;
                    return;
                }

                Vector2 pointerPosition = activePointer.Position;
                Vector2 pointerDelta = pointerPosition - lastPointerPosition;
                lastPointerPosition = pointerPosition;
                ref Position position = ref world.GetComponent<Position>(resizingEntity, positionType);
                ref Scale scale = ref world.GetComponent<Scale>(resizingEntity, scaleType);

                if (resizeBoundary.HasFlag(IsResizable.EdgeMask.Right))
                {
                    scale.value.X += pointerDelta.X;
                }
                else if (resizeBoundary.HasFlag(IsResizable.EdgeMask.Left))
                {
                    scale.value.X -= pointerDelta.X;
                    position.value.X += pointerDelta.X;
                }

                if (resizeBoundary.HasFlag(IsResizable.EdgeMask.Top))
                {
                    scale.value.Y += pointerDelta.Y;
                }
                else if (resizeBoundary.HasFlag(IsResizable.EdgeMask.Bottom))
                {
                    scale.value.Y -= pointerDelta.Y;
                    position.value.Y += pointerDelta.Y;
                }
            }
        }
    }
}