using Collections.Generic;
using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using UI.Components;
using UI.Messages;
using Worlds;

namespace UI.Systems
{
    public partial class PointerDraggingSelectableSystem : SystemBase, IListener<UIUpdate>
    {
        private readonly World world;
        private readonly List<uint> pressedStates;
        private readonly List<uint> draggableEntities;
        private readonly int pointerType;
        private readonly int positionType;

        private uint dragTarget;
        private uint dragPointer;
        private Vector2 lastPosition;

        public PointerDraggingSelectableSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            pressedStates = new(16);
            draggableEntities = new(16);
            dragTarget = default;
            dragPointer = default;
            lastPosition = default;

            Schema schema = world.Schema;
            pointerType = schema.GetComponentType<IsPointer>();
            positionType = schema.GetComponentType<Position>();
        }

        public override void Dispose()
        {
            draggableEntities.Dispose();
            pressedStates.Dispose();
        }

        void IListener<UIUpdate>.Receive(ref UIUpdate message)
        {
            FindDraggableEntities();

            ComponentQuery<IsPointer> query = new(world);
            query.ExcludeDisabled(true);
            foreach (var r in query)
            {
                ref IsPointer pointer = ref r.component1;
                bool pressed = pointer.HasPrimaryIntent;
                bool wasPressed = false;
                if (pressed)
                {
                    wasPressed = pressedStates.TryAdd(r.entity);
                }
                else
                {
                    pressedStates.TryRemoveBySwapping(r.entity);
                }

                Vector2 position = pointer.position;
                if (wasPressed && dragTarget == default)
                {
                    rint hoveringOverReference = pointer.hoveringOverReference;
                    if (hoveringOverReference != default)
                    {
                        uint hoveringOverEntity = world.GetReference(r.entity, hoveringOverReference);
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
                                dragTarget = targetEntity;
                                dragPointer = r.entity;
                                lastPosition = position;
                            }
                        }
                    }
                }
                else if (!pressed && r.entity == dragPointer)
                {
                    dragTarget = default;
                }
            }

            if (dragTarget != default && world.ContainsEntity(dragTarget)) //todo: checking for default is unecessary im pretty sure
            {
                Vector2 position = world.GetComponent<IsPointer>(dragPointer, pointerType).position;
                Vector2 pointerDelta = position - lastPosition;
                lastPosition = position;

                ref Position selectablePosition = ref world.TryGetComponent<Position>(dragTarget, positionType, out bool contains);
                if (contains)
                {
                    selectablePosition.value += new Vector3(pointerDelta, 0);
                }
            }
        }

        private void FindDraggableEntities()
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