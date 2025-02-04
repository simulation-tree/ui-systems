using Collections;
using UI.Components;
using Rendering;
using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using Worlds;

namespace UI.Systems
{
    public partial struct ResizingSystem : ISystem
    {
        private readonly Dictionary<Entity, bool> lastPressedPointers;
        private Entity resizingEntity;
        private IsResizable.Boundary resizeBoundary;
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
            ComponentQuery<IsPointer> pointerQuery = new(world);
            pointerQuery.ExcludeDisabled(true);
            foreach (var p in pointerQuery)
            {
                Entity entity = new(world, p.entity);
                if (!lastPressedPointers.ContainsKey(entity))
                {
                    lastPressedPointers.Add(entity, p.component1.HasPrimaryIntent);
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
                    foreach (var p in pointerQuery)
                    {
                        ref IsPointer pointer = ref p.component1;
                        LayerMask pointerSelectionMask = pointer.selectionMask;
                        if (pointerSelectionMask.ContainsAll(resizableMask))
                        {
                            if (pointer.HasPrimaryIntent && !lastPressedPointers[new(world, p.entity)])
                            {
                                Vector2 pointerPosition = pointer.position;
                                IsResizable.Boundary boundary = resizable.GetBoundary(pointerPosition);
                                if (boundary != default)
                                {
                                    resizingEntity = resizable;
                                    resizeBoundary = boundary;
                                    activePointer = new Entity(world, p.entity).As<Pointer>();
                                    lastPointerPosition = pointerPosition;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var p in pointerQuery)
            {
                lastPressedPointers[new(world, p.entity)] = p.component1.HasPrimaryIntent;
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

                if (resizeBoundary.HasFlag(IsResizable.Boundary.Right))
                {
                    scale.value.X += pointerDelta.X;
                }
                else if (resizeBoundary.HasFlag(IsResizable.Boundary.Left))
                {
                    scale.value.X -= pointerDelta.X;
                    position.value.X += pointerDelta.X;
                }

                if (resizeBoundary.HasFlag(IsResizable.Boundary.Top))
                {
                    scale.value.Y += pointerDelta.Y;
                }
                else if (resizeBoundary.HasFlag(IsResizable.Boundary.Bottom))
                {
                    scale.value.Y -= pointerDelta.Y;
                    position.value.Y += pointerDelta.Y;
                }
            }
        }
    }
}