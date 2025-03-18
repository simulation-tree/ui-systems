using Automations;
using Automations.Components;
using Collections.Generic;
using Simulation;
using System;
using UI.Components;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct AutomationParameterSystem : ISystem
    {
        private readonly List<Entity> selectedEntities;

        public AutomationParameterSystem()
        {
            selectedEntities = new(16);
        }

        public readonly void Dispose()
        {
            selectedEntities.Dispose();
        }

        readonly void ISystem.Start(in SystemContext context, in World world)
        {
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            FindSelectedEntities(world);
            UpdateSelectableParameters(world);
        }

        readonly void ISystem.Finish(in SystemContext context, in World world)
        {
        }

        private readonly void FindSelectedEntities(World world)
        {
            selectedEntities.Clear();

            ComponentQuery<IsPointer> pointerQuery = new(world);
            pointerQuery.ExcludeDisabled(true);
            foreach (var p in pointerQuery)
            {
                ref IsPointer pointer = ref p.component1;
                if (pointer.hoveringOverReference != default)
                {
                    uint hoveringOverEntity = world.GetReference(p.entity, pointer.hoveringOverReference);
                    if (world.ContainsEntity(hoveringOverEntity))
                    {
                        selectedEntities.Add(new(world, hoveringOverEntity));
                    }
                }
            }
        }

        private readonly void UpdateSelectableParameters(World world)
        {
            ComponentQuery<IsSelectable, IsStateful> selectablesQuery = new(world);
            selectablesQuery.ExcludeDisabled(true);
            foreach (var x in selectablesQuery)
            {
                ref IsSelectable selectable = ref x.component1;
                bool pressed = (selectable.state & IsSelectable.State.WasPrimaryInteractedWith) != 0;
                pressed |= (selectable.state & IsSelectable.State.IsPrimaryInteractedWith) != 0;
                Entity entity = new(world, x.entity);
                Stateful stateful = entity.As<Stateful>();
                bool selected = selectedEntities.Contains(entity);
                stateful.SetParameter("selected", selected ? 1 : 0);
                stateful.SetParameter("pressed", pressed ? 1 : 0);
            }
        }
    }
}