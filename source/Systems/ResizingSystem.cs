using Collections;
using UI.Components;
using Rendering;
using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using Worlds;
using System.Text;
using Unmanaged;

namespace UI.Systems
{
    public partial struct ResizingSystem : ISystem
    {
        private readonly Dictionary<Entity, bool> lastPressedPointers;
        private Entity resizingEntity;
        private IsResizable.EdgeMask resizeBoundary;
        private Pointer activePointer;
        private Vector2 lastPointerPosition;

        public ResizingSystem()
        {
            lastPressedPointers = new();
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                lastPressedPointers.Dispose();
            }
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                systemContainer.Write(new ResizingSystem());
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            ComponentType pointerType = world.Schema.GetComponent<IsPointer>();
            ComponentType resizableType = world.Schema.GetComponent<IsResizable>();
            ComponentType positionType = world.Schema.GetComponent<Position>();
            ComponentType scaleType = world.Schema.GetComponent<Scale>();
            const int PointerCapacity = 16;
            USpan<uint> pointerEntities = stackalloc uint[PointerCapacity];
            USpan<IsPointer> pointerComponents = stackalloc IsPointer[PointerCapacity];
            uint pointerEntityCount = 0;
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.Contains(pointerType) && !definition.Contains(TagType.Disabled))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<IsPointer> components = chunk.GetComponents<IsPointer>(pointerType);
                    for (uint i = 0; i < entities.Length; i++)
                    {
                        Entity entity = new(world, entities[i]);
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
                    Resizable resizable = new Entity(world, r.entity).As<Resizable>();
                    LayerMask resizableMask = r.component1.selectionMask;
                    for (uint p = 0; p < pointerEntityCount; p++)
                    {
                        ref IsPointer pointer = ref pointerComponents[p];
                        LayerMask pointerSelectionMask = pointer.selectionMask;
                        if (pointerSelectionMask.ContainsAll(resizableMask))
                        {
                            uint pointerEntity = pointerEntities[p];
                            if (pointer.HasPrimaryIntent && !lastPressedPointers[new(world, pointerEntity)])
                            {
                                Vector2 pointerPosition = pointer.position;
                                IsResizable.EdgeMask boundary = resizable.GetBoundary(pointerPosition);
                                if (boundary != default)
                                {
                                    resizingEntity = resizable;
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

            for (uint p = 0; p < pointerEntityCount; p++)
            {
                ref IsPointer pointer = ref pointerComponents[p];
                lastPressedPointers[new(world, pointerEntities[p])] = pointer.HasPrimaryIntent;
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
                ref Position position = ref resizingEntity.GetComponent<Position>();
                ref Scale scale = ref resizingEntity.GetComponent<Scale>();

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