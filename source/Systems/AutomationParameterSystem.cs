using Automations;
using Automations.Components;
using Collections.Generic;
using Simulation;
using System;
using UI.Components;
using UI.Messages;
using Worlds;

namespace UI.Systems
{
    public partial class AutomationParameterSystem : SystemBase, IListener<UIUpdate>
    {
        private readonly World world;
        private readonly List<uint> selectedEntities;

        public AutomationParameterSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            selectedEntities = new(16);
        }

        public override void Dispose()
        {
            selectedEntities.Dispose();
        }

        void IListener<UIUpdate>.Receive(ref UIUpdate message)
        {
            FindSelectedEntities();
            UpdateSelectableParameters();
        }

        private void FindSelectedEntities()
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
                        selectedEntities.Add(hoveringOverEntity);
                    }
                }
            }
        }

        private void UpdateSelectableParameters()
        {
            Span<uint> selectedEntities = this.selectedEntities.AsSpan();
            ComponentQuery<IsSelectable, IsStateful> selectablesQuery = new(world);
            selectablesQuery.ExcludeDisabled(true);
            foreach (var x in selectablesQuery)
            {
                ref IsSelectable selectable = ref x.component1;
                bool pressed = (selectable.state & IsSelectable.State.WasPrimaryInteractedWith) != 0;
                pressed |= (selectable.state & IsSelectable.State.IsPrimaryInteractedWith) != 0;
                Stateful stateful = Entity.Get<Stateful>(world, x.entity);
                bool selected = selectedEntities.Contains(x.entity);
                stateful.SetParameter("selected", selected ? 1 : 0);
                stateful.SetParameter("pressed", pressed ? 1 : 0);
            }
        }
    }
}