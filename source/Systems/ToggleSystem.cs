using Collections.Generic;
using Simulation;
using System;
using UI.Components;
using Worlds;

namespace UI.Systems
{
    public class ToggleSystem : ISystem, IDisposable
    {
        private readonly List<uint> pressedPointers;
        private readonly List<uint> toggleEntities;

        public ToggleSystem()
        {
            pressedPointers = new(4);
            toggleEntities = new(4);
        }

        public void Dispose()
        {
            toggleEntities.Dispose();
            pressedPointers.Dispose();
        }

        void ISystem.Update(Simulator simulator, double deltaTime)
        {
            World world = simulator.world;
            FindToggleEntities(world);

            Span<uint> toggleEntities = this.toggleEntities.AsSpan();
            int pointerComponent = world.Schema.GetComponentType<IsPointer>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(pointerComponent) && !definition.ContainsTag(Schema.DisabledTagType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsPointer> components = chunk.GetComponents<IsPointer>(pointerComponent);
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
                                        ref IsToggle component = ref world.GetComponent<IsToggle>(selectedEntity);
                                        component.value = !component.value;

                                        rint checkmarkReference = component.checkmarkReference;
                                        uint checkmarkEntity = world.GetReference(selectedEntity, checkmarkReference);
                                        world.SetEnabled(checkmarkEntity, component.value);

                                        if (component.callback != default)
                                        {
                                            Toggle toggle = new Entity(world, selectedEntity).As<Toggle>();
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

        private void FindToggleEntities(World world)
        {
            toggleEntities.Clear();
            int toggleComponent = world.Schema.GetComponentType<IsToggle>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(toggleComponent) && !definition.ContainsTag(Schema.DisabledTagType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    toggleEntities.AddRange(entities);
                }
            }
        }
    }
}