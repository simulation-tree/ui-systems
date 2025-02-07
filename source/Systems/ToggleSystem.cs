using Collections;
using UI.Components;
using Simulation;
using System;
using Worlds;
using Unmanaged;

namespace UI.Systems
{
    public readonly partial struct ToggleSystem : ISystem
    {
        private readonly List<uint> pressedPointers;
        private readonly List<uint> toggleEntities;

        private ToggleSystem(List<uint> pressedPointers, List<uint> toggleEntities)
        {
            this.pressedPointers = pressedPointers;
            this.toggleEntities = toggleEntities;
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                List<uint> pressedPointers = new();
                List<uint> toggleEntities = new();
                systemContainer.Write(new ToggleSystem(pressedPointers, toggleEntities));
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            Update(world);
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                toggleEntities.Dispose();
                pressedPointers.Dispose();
            }
        }

        private readonly void Update(World world)
        {
            FindToggleEntities(world);

            ComponentType pointerComponent = world.Schema.GetComponent<IsPointer>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.Contains(pointerComponent) && !definition.Contains(TagType.Disabled))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<IsPointer> components = chunk.GetComponents<IsPointer>(pointerComponent);
                    for (uint i = 0; i < entities.Length; i++)
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

        private readonly void FindToggleEntities(World world)
        {
            toggleEntities.Clear();
            ComponentType toggleComponent = world.Schema.GetComponent<IsToggle>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.Contains(toggleComponent) && !definition.Contains(TagType.Disabled))
                {
                    USpan<uint> entities = chunk.Entities;
                    toggleEntities.AddRange(entities);
                }
            }
        }
    }
}