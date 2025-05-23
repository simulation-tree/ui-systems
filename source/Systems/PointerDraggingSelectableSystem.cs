using Collections.Generic;
using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using UI.Components;
using Worlds;

namespace UI.Systems
{
    public class PointerDraggingSelectableSystem : ISystem, IDisposable
    {
        private readonly List<Entity> pressedStates;
        private readonly List<uint> draggableEntities;

        private Entity dragTarget;
        private Entity dragPointer;
        private Vector2 lastPosition;

        public PointerDraggingSelectableSystem()
        {
            pressedStates = new(16);
            draggableEntities = new(16);
            dragTarget = default;
            dragPointer = default;
            lastPosition = default;
        }

        public void Dispose()
        {
            draggableEntities.Dispose();
            pressedStates.Dispose();
        }

        void ISystem.Update(Simulator simulator, double deltaTime)
        {
            World world = simulator.world;
            FindDraggableEntities(world);

            ComponentQuery<IsPointer> query = new(world);
            query.ExcludeDisabled(true);
            foreach (var r in query)
            {
                ref IsPointer pointer = ref r.component1;
                Entity entity = new(world, r.entity);
                bool pressed = pointer.HasPrimaryIntent;
                bool wasPressed = false;
                if (pressed)
                {
                    wasPressed = pressedStates.TryAdd(entity);
                }
                else
                {
                    pressedStates.TryRemoveBySwapping(entity);
                }

                Vector2 position = pointer.position;
                if (wasPressed && dragTarget == default)
                {
                    rint hoveringOverReference = pointer.hoveringOverReference;
                    if (hoveringOverReference != default)
                    {
                        uint hoveringOverEntity = entity.GetReference(hoveringOverReference);
                        if (draggableEntities.Contains(hoveringOverEntity))
                        {
                            rint targetReference = world.GetComponent<IsDraggable>(hoveringOverEntity).targetReference;
                            uint targetEntity = targetReference == default ? default : world.GetReference(hoveringOverEntity, targetReference);
                            if (targetEntity == default)
                            {
                                targetEntity = hoveringOverEntity;
                            }

                            if (targetEntity != default && world.ContainsEntity(targetEntity))
                            {
                                dragTarget = new(world, targetEntity);
                                dragPointer = entity;
                                lastPosition = position;
                            }
                        }
                    }
                }
                else if (!pressed && entity == dragPointer)
                {
                    dragTarget = default;
                }
            }

            if (dragTarget != default && !dragTarget.IsDestroyed)
            {
                Vector2 position = dragPointer.GetComponent<IsPointer>().position;
                Vector2 pointerDelta = position - lastPosition;
                lastPosition = position;

                ref Position selectablePosition = ref dragTarget.TryGetComponent<Position>(out bool contains);
                if (contains)
                {
                    selectablePosition.value += new Vector3(pointerDelta, 0);
                }
            }
        }

        private void FindDraggableEntities(World world)
        {
            draggableEntities.Clear();

            ComponentQuery<IsDraggable> query = new(world);
            query.ExcludeDisabled(true);
            foreach (var r in query)
            {
                draggableEntities.Add(r.entity);
            }
        }
    }
}