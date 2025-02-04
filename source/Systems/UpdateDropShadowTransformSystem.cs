using UI.Components;
using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct UpdateDropShadowTransformSystem : ISystem
    {
        private readonly Operation destroyOperation;

        private UpdateDropShadowTransformSystem(Operation destroyOperation)
        {
            this.destroyOperation = destroyOperation;
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                destroyOperation.Dispose();
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
            ComponentQuery<IsDropShadow> query = new(world);
            foreach (var r in query)
            {
                ref IsDropShadow component = ref r.component1;
                uint foregroundEntity = world.GetReference(r.entity, component.foregroundReference);
                if (foregroundEntity == default || !world.ContainsEntity(foregroundEntity))
                {
                    destroyOperation.SelectEntity(r.entity);
                }
                else
                {
                    world.SetEnabled(r.entity, world.IsEnabled(foregroundEntity));
                }
            }

            if (destroyOperation.Count > 0)
            {
                destroyOperation.DestroySelected();
                destroyOperation.ClearSelection();
            }

            Schema schema = world.Schema;
            ComponentQuery<IsDropShadow> queryWithoutScale = new(world);
            queryWithoutScale.ExcludeComponent<Scale>();
            foreach (var r in queryWithoutScale)
            {
                destroyOperation.SelectEntity(r.entity);
            }

            if (destroyOperation.HasSelection)
            {
                destroyOperation.AddComponent(Scale.Default, schema);
            }

            if (destroyOperation.Count > 0)
            {
                world.Perform(destroyOperation);
                destroyOperation.Clear();
            }

            const float ShadowDistance = 30f;
            ComponentQuery<IsDropShadow, Position, Scale> fullQuery = new(world);
            foreach (var r in fullQuery)
            {
                ref IsDropShadow component = ref r.component1;
                ref Position position = ref r.component2;
                ref Scale scale = ref r.component3;

                //make the dropshadow match the foreground in world space
                rint foregroundReference = component.foregroundReference;
                uint foregroundEntity = world.GetReference(r.entity, foregroundReference);
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
                uint meshEntity = world.GetReference(r.entity, meshReference);
                ref LocalToWorld meshLtw = ref world.GetComponent<LocalToWorld>(meshEntity);
                meshLtw = new(positionValue, Quaternion.Identity, scaleValue);
            }
        }
    }
}