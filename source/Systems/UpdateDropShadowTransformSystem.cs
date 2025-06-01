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
    public partial class UpdateDropShadowTransformSystem : SystemBase, IListener<UIUpdate>
    {
        private readonly World world;
        private readonly Operation operation;
        private readonly int positionComponent;
        private readonly int scaleComponent;
        private readonly int dropShadowComponent;
        private readonly int ltwType;
        private readonly int menuType;
        private readonly int menuOptionArrayType;

        public UpdateDropShadowTransformSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            operation = new(world);

            Schema schema = world.Schema;
            positionComponent = schema.GetComponentType<Position>();
            scaleComponent = schema.GetComponentType<Scale>();
            dropShadowComponent = schema.GetComponentType<IsDropShadow>();
            ltwType = schema.GetComponentType<LocalToWorld>();
            menuType = schema.GetComponentType<IsMenu>();
            menuOptionArrayType = schema.GetArrayType<IsMenuOption>();
        }

        public override void Dispose()
        {
            operation.Dispose();
        }

        void IListener<UIUpdate>.Receive(ref UIUpdate message)
        {
            //destroy drop shadows if their foreground doesnt exist anymore
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
                            operation.AppendEntityToSelection(entity);
                        }
                        else
                        {
                            bool foregroundEnabled = world.IsEnabled(foregroundEntity);
                            setEnabled[setEnabledCount++] = (entity, foregroundEnabled);
                        }
                    }
                }
            }

            //todo: shouldnt this be an operation?
            for (int i = 0; i < setEnabledCount; i++)
            {
                (uint entity, bool enabled) = setEnabled[i];
                world.SetEnabled(entity, enabled);
            }

            if (operation.Count > 0)
            {
                operation.DestroySelectedEntities();
                operation.ClearSelection();
            }

            //add scale component to drop shadows that dont have it yet
            //todo: should check if the entity has been destroyed since previous instructions
            bool selectedAny = false;
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Count > 0)
                {
                    Definition definition = chunk.Definition;
                    if (definition.ContainsComponent(dropShadowComponent) && !definition.ContainsComponent(scaleComponent))
                    {
                        ReadOnlySpan<uint> entities = chunk.Entities;
                        operation.AppendMultipleEntitiesToSelection(entities);
                        selectedAny = true;
                    }
                }
            }

            if (selectedAny)
            {
                operation.AddComponent(Scale.Default);
            }

            if (operation.TryPerform())
            {
                operation.Reset();
            }

            //do the thing
            const float ShadowDistance = 30f;
            BitMask componentTypes = new(dropShadowComponent, scaleComponent, positionComponent);
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.componentTypes.ContainsAll(componentTypes))
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
                        ref LocalToWorld foregroundLtw = ref world.GetComponent<LocalToWorld>(foregroundEntity, ltwType);
                        Vector3 positionValue = foregroundLtw.Position;
                        Vector3 scaleValue = foregroundLtw.Scale;
                        if (world.TryGetComponent(foregroundEntity, menuType, out IsMenu menuComponent))
                        {
                            //unique branch for menus
                            int optionCount = world.GetArrayLength(foregroundEntity, menuOptionArrayType);
                            float originalHeight = menuComponent.optionSize.Y;
                            scaleValue.X = menuComponent.optionSize.X;
                            scaleValue.Y = originalHeight * optionCount;
                            positionValue.Y -= originalHeight * optionCount;
                        }

                        position.value = positionValue + new Vector3(-ShadowDistance, -ShadowDistance, Settings.ZScale * -1.1f);
                        scale.value = scaleValue + new Vector3(ShadowDistance * 2f, ShadowDistance * 2f, 1f);

                        //update the ltw of the mesh to match foreground
                        rint meshReference = component.meshReference;
                        uint meshEntity = world.GetReference(entity, meshReference);
                        ref LocalToWorld meshLtw = ref world.GetComponent<LocalToWorld>(meshEntity, ltwType);
                        meshLtw = new(positionValue, Quaternion.Identity, scaleValue);
                    }
                }
            }
        }
    }
}