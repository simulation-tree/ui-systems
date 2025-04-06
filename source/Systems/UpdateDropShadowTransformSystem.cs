using Simulation;
using System;
using System.Diagnostics;
using System.Numerics;
using Transforms.Components;
using UI.Components;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct UpdateDropShadowTransformSystem : ISystem
    {
        private readonly Operation operation;

        public UpdateDropShadowTransformSystem()
        {
            operation = new();
        }

        public readonly void Dispose()
        {
            operation.Dispose();
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            //destroy drop shadows if their foreground doesnt exist anymore
            int dropShadowComponent = world.Schema.GetComponentType<IsDropShadow>();
            Span<(uint, bool)> setEnabled = stackalloc (uint, bool)[256];
            int setEnabledCount = 0;
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(dropShadowComponent))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsDropShadow> components = chunk.GetComponents<IsDropShadow>(dropShadowComponent);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        uint entity = entities[i];
                        ref IsDropShadow component = ref components[i];
                        uint foregroundEntity = world.GetReference(entity, component.foregroundReference);
                        if (foregroundEntity == default || !world.ContainsEntity(foregroundEntity))
                        {
                            operation.SelectEntity(entity);
                        }
                        else
                        {
                            bool foregroundEnabled = world.IsEnabled(foregroundEntity);
                            setEnabled[setEnabledCount++] = (entity, foregroundEnabled);
                        }
                    }
                }
            }

            for (int i = 0; i < setEnabledCount; i++)
            {
                (uint entity, bool enabled) = setEnabled[i];
                world.SetEnabled(entity, enabled);
            }

            if (operation.Count > 0)
            {
                operation.DestroySelected();
                operation.ClearSelection();
            }

            //add scale component to drop shadows that dont have it yet
            //todo: should check if the entity has been destroyed since previous instructions
            bool selectedAny = false;
            int scaleComponent = world.Schema.GetComponentType<Scale>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(dropShadowComponent) && !definition.ContainsComponent(scaleComponent))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    operation.SelectEntities(entities);
                    selectedAny = true;
                }
            }

            if (selectedAny)
            {
                operation.AddComponent(Scale.Default);
            }


            if (operation.Count > 0)
            {
                operation.Perform(world);
                operation.Clear();
            }

            //do the thing
            const float ShadowDistance = 30f;
            int positionComponent = world.Schema.GetComponentType<Position>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(dropShadowComponent) && definition.ContainsComponent(scaleComponent) && definition.ContainsComponent(positionComponent))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsDropShadow> dropShadowComponents = chunk.GetComponents<IsDropShadow>(dropShadowComponent);
                    ComponentEnumerator<Position> positionComponents = chunk.GetComponents<Position>(positionComponent);
                    ComponentEnumerator<Scale> scaleComponents = chunk.GetComponents<Scale>(scaleComponent);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        uint entity = entities[i];
                        ref IsDropShadow component = ref dropShadowComponents[i];
                        ref Position position = ref positionComponents[i];
                        ref Scale scale = ref scaleComponents[i];

                        //make the dropshadow match the foreground in world space
                        rint foregroundReference = component.foregroundReference;
                        uint foregroundEntity = world.GetReference(entity, foregroundReference);
                        ref LocalToWorld foregroundLtw = ref world.GetComponent<LocalToWorld>(foregroundEntity);
                        Vector3 positionValue = foregroundLtw.Position;
                        Vector3 scaleValue = foregroundLtw.Scale;
                        if (world.TryGetComponent(foregroundEntity, out IsMenu menuComponent))
                        {
                            //unique branch for menus
                            int optionCount = world.GetArrayLength<IsMenuOption>(foregroundEntity);
                            float originalHeight = menuComponent.optionSize.Y;
                            scaleValue.X = menuComponent.optionSize.X;
                            scaleValue.Y = originalHeight * optionCount;
                            positionValue.Y -= originalHeight * optionCount;
                        }

                        position.value = positionValue + new Vector3(-ShadowDistance, -ShadowDistance, Settings.ZScale * -2f);
                        scale.value = scaleValue + new Vector3(ShadowDistance * 2f, ShadowDistance * 2f, 1f);

                        //update the ltw of the mesh to match foreground
                        rint meshReference = component.meshReference;
                        uint meshEntity = world.GetReference(entity, meshReference);
                        ref LocalToWorld meshLtw = ref world.GetComponent<LocalToWorld>(meshEntity);
                        meshLtw = new(positionValue, Quaternion.Identity, scaleValue);
                    }
                }
            }
        }
    }
}