using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using UI.Components;
using Unmanaged;
using Worlds;
using static Worlds.Chunk;

namespace UI.Systems
{
    public readonly partial struct UpdateDropShadowTransformSystem : ISystem
    {
        private readonly Operation operation;

        private UpdateDropShadowTransformSystem(Operation destroyOperation)
        {
            this.operation = destroyOperation;
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                operation.Dispose();
            }
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                systemContainer.Write(new UpdateDropShadowTransformSystem(new()));
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            //destroy drop shadows if their foreground doesnt exist anymore
            ComponentType dropShadowComponent = world.Schema.GetComponentType<IsDropShadow>();
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(dropShadowComponent))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<IsDropShadow> components = chunk.GetComponents<IsDropShadow>(dropShadowComponent);
                    for (uint i = 0; i < entities.Length; i++)
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
                            world.SetEnabled(entity, world.IsEnabled(foregroundEntity));
                        }
                    }
                }
            }

            if (operation.Count > 0)
            {
                operation.DestroySelected();
                operation.ClearSelection();
            }

            //add scale component to drop shadows that dont have it yet
            //todo: should check if the entity has been destroyed since previous instructions
            bool selectedAny = false;
            ComponentType scaleComponent = world.Schema.GetComponentType<Scale>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(dropShadowComponent) && !definition.ContainsComponent(scaleComponent))
                {
                    USpan<uint> entities = chunk.Entities;
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
            ComponentType positionComponent = world.Schema.GetComponentType<Position>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(dropShadowComponent) && definition.ContainsComponent(scaleComponent) && definition.ContainsComponent(positionComponent))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<IsDropShadow> dropShadowComponents = chunk.GetComponents<IsDropShadow>(dropShadowComponent);
                    USpan<Position> positionComponents = chunk.GetComponents<Position>(positionComponent);
                    USpan<Scale> scaleComponents = chunk.GetComponents<Scale>(scaleComponent);
                    for (uint i = 0; i < entities.Length; i++)
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
                            uint optionCount = world.GetArrayLength<IsMenuOption>(foregroundEntity);
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