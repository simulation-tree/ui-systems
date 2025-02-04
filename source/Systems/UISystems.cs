using Simulation;
using System;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct UISystems : ISystem
    {
        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                Simulator simulator = systemContainer.simulator;
                simulator.AddSystem<CanvasSystem>();
                simulator.AddSystem<SelectionSystem>();
                simulator.AddSystem<ResizingSystem>();
                simulator.AddSystem<LabelTextSystem>();
                simulator.AddSystem<VirtualWindowsScrollViewSystem>();
                simulator.AddSystem<InvokeTriggersSystem>();
                simulator.AddSystem<AutomationParameterSystem>();
                simulator.AddSystem<ComponentMixingSystem>();
                simulator.AddSystem<PointerDraggingSelectableSystem>();
                simulator.AddSystem<ToggleSystem>();
                simulator.AddSystem<ScrollHandleMovingSystem>();
                simulator.AddSystem<ScrollViewSystem>();
                simulator.AddSystem<TextFieldEditingSystem>();
                simulator.AddSystem<DropdownMenusSystem>();
                simulator.AddSystem<UpdateDropShadowTransformSystem>();
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                Simulator simulator = systemContainer.simulator;
                simulator.RemoveSystem<UpdateDropShadowTransformSystem>();
                simulator.RemoveSystem<DropdownMenusSystem>();
                simulator.RemoveSystem<TextFieldEditingSystem>();
                simulator.RemoveSystem<ScrollViewSystem>();
                simulator.RemoveSystem<ScrollHandleMovingSystem>();
                simulator.RemoveSystem<ToggleSystem>();
                simulator.RemoveSystem<PointerDraggingSelectableSystem>();
                simulator.RemoveSystem<ComponentMixingSystem>();
                simulator.RemoveSystem<AutomationParameterSystem>();
                simulator.RemoveSystem<InvokeTriggersSystem>();
                simulator.RemoveSystem<VirtualWindowsScrollViewSystem>();
                simulator.RemoveSystem<LabelTextSystem>();
                simulator.RemoveSystem<ResizingSystem>();
                simulator.RemoveSystem<SelectionSystem>();
                simulator.RemoveSystem<CanvasSystem>();
            }
        }
    }
}