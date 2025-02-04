using UI.Components;
using Simulation;
using System;
using Transforms.Components;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct DropdownMenusSystem : ISystem
    {
        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            ComponentQuery<IsDropdown, LocalToWorld> dropdownQuery = new(world);
            dropdownQuery.ExcludeDisabled(true);
            foreach (var r in dropdownQuery)
            {
                uint entity = r.entity;
                ref IsDropdown dropdown = ref r.component1;
                ref LocalToWorld ltw = ref r.component2;
                uint menuEntity = world.GetReference(entity, dropdown.menuReference);
                ref Position menuPosition = ref world.TryGetComponent<Position>(menuEntity, out bool contains);
                if (contains)
                {
                    menuPosition.value = ltw.Position;
                    ref LocalToWorld menuLtw = ref world.GetComponent<LocalToWorld>(menuEntity);
                }
            }
        }
    }
}