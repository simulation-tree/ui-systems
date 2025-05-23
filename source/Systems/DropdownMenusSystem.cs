using Simulation;
using Transforms.Components;
using UI.Components;
using Worlds;

namespace UI.Systems
{
    public class DropdownMenusSystem : ISystem
    {
        void ISystem.Update(Simulator simulator, double deltaTime)
        {
            World world = simulator.world;
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