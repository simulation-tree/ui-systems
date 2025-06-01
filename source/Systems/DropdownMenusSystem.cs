using Simulation;
using Transforms.Components;
using UI.Components;
using UI.Messages;
using Worlds;

namespace UI.Systems
{
    public partial class DropdownMenusSystem : SystemBase, IListener<UIUpdate>
    {
        private readonly World world;
        private readonly int dropdownType;
        private readonly int positionType;
        private readonly int ltwType;

        public DropdownMenusSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            Schema schema = world.Schema;
            dropdownType = schema.GetComponentType<IsDropdown>();
            positionType = schema.GetComponentType<Position>();
            ltwType = schema.GetComponentType<LocalToWorld>();
        }

        public override void Dispose()
        {
        }

        void IListener<UIUpdate>.Receive(ref UIUpdate message)
        {
            ComponentQuery<IsDropdown, LocalToWorld> dropdownQuery = new(world);
            dropdownQuery.ExcludeDisabled(true);
            foreach (var r in dropdownQuery)
            {
                uint entity = r.entity;
                ref IsDropdown dropdown = ref r.component1;
                ref LocalToWorld ltw = ref r.component2;
                uint menuEntity = world.GetReference(entity, dropdown.menuReference);
                ref Position menuPosition = ref world.TryGetComponent<Position>(menuEntity, positionType, out bool contains);
                if (contains)
                {
                    menuPosition.value = ltw.Position;
                    //ref LocalToWorld menuLtw = ref world.GetComponent<LocalToWorld>(menuEntity, ltwType); //todo: dont remember why this is here
                }
            }
        }
    }
}