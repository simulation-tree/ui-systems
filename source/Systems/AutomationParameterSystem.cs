using Automations;
using Automations.Components;
using Collections.Generic;
using Simulation;
using System;
using UI.Components;
using Worlds;

namespace UI.Systems
{
    public class AutomationParameterSystem : ISystem, IDisposable
    {
        private readonly List<Entity> selectedEntities;

        public AutomationParameterSystem()
        {
            selectedEntities = new(16);
        }

        public void Dispose()
        {
            selectedEntities.Dispose();
        }

        void ISystem.Update(Simulator simulator, double deltaTime)
        {
            FindSelectedEntities(simulator.world);
            UpdateSelectableParameters(simulator.world);
        }

        private void FindSelectedEntities(World world)
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

        private void UpdateSelectableParameters(World world)
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