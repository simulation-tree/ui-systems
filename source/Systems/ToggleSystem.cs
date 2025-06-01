using Collections.Generic;
using Simulation;
using System;
using UI.Components;
using UI.Messages;
using Worlds;

namespace UI.Systems
{
    public partial class ToggleSystem : SystemBase, IListener<UIUpdate>
    {
        private readonly World world;
        private readonly List<uint> pressedPointers;
        private readonly List<uint> toggleEntities;
        private readonly int toggleType;
        private readonly int pointerType;

        public ToggleSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            pressedPointers = new(4);
            toggleEntities = new(4);

            Schema schema = world.Schema;
            toggleType = schema.GetComponentType<IsToggle>();
            pointerType = world.Schema.GetComponentType<IsPointer>();
        }

        public override void Dispose()
        {
            toggleEntities.Dispose();
            pressedPointers.Dispose();
        }

        void IListener<UIUpdate>.Receive(ref UIUpdate message)
        {
            FindToggleEntities();
            Span<uint> toggleEntities = this.toggleEntities.AsSpan();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(pointerType) && !definition.IsDisabled)
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsPointer> components = chunk.GetComponents<IsPointer>(pointerType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsPointer pointer = ref components[i];
                        uint entity = entities[i];
                        bool pressed = pointer.HasPrimaryIntent;
                        bool wasPressed = pressedPointers.Contains(entity);
                        if (wasPressed != pressed)
                        {
                            if (pressed)
                            {
                                rint hoveringOverReference = pointer.hoveringOverReference;
                                if (hoveringOverReference != default)
                                {
                                    uint selectedEntity = world.GetReference(entity, hoveringOverReference);
                                    if (toggleEntities.Contains(selectedEntity))
                                    {
                                        ref IsToggle component = ref world.GetComponent<IsToggle>(selectedEntity, toggleType);
                                        component.value = !component.value;

                                        rint checkmarkReference = component.checkmarkReference;
                                        uint checkmarkEntity = world.GetReference(selectedEntity, checkmarkReference);
                                        world.SetEnabled(checkmarkEntity, component.value);

                                        if (component.callback != default)
                                        {
                                            Toggle toggle = Entity.Get<Toggle>(world, selectedEntity);
                                            component.callback.Invoke(toggle, component.value);
                                        }
                                    }
                                }

                                pressedPointers.Add(entity);
                            }
                            else
                            {
                                pressedPointers.TryRemoveBySwapping(entity);
                            }
                        }
                    }
                }
            }
        }

        private void FindToggleEntities()
        {
            toggleEntities.Clear();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(toggleType) && !definition.IsDisabled)
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    toggleEntities.AddRange(entities);
                }
            }
        }
    }
}