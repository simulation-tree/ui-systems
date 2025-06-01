using Rendering;
using Simulation;
using System;
using UI.Systems.RenderEnginePlugins;
using Worlds;

namespace UI.Systems
{
    public class UISystemsBank : IDisposable
    {
        public readonly Simulator simulator;

        public UISystemsBank(Simulator simulator, World world)
        {
            this.simulator = simulator;
            simulator.Add(new CanvasSystem(simulator, world));
            simulator.Add(new SelectionSystem(simulator, world));
            simulator.Add(new ResizingSystem(simulator, world));
            simulator.Add(new LabelTextSystem(simulator, world));
            simulator.Add(new VirtualWindowsScrollViewSystem(simulator, world));
            simulator.Add(new InvokeTriggersSystem(simulator, world));
            simulator.Add(new AutomationParameterSystem(simulator, world));
            simulator.Add(new ComponentMixingSystem(simulator, world));
            simulator.Add(new PointerDraggingSelectableSystem(simulator, world));
            simulator.Add(new ToggleSystem(simulator, world));
            simulator.Add(new ScrollHandleMovingSystem(simulator, world));
            simulator.Add(new ScrollViewSystem(simulator, world));
            simulator.Add(new TextFieldEditingSystem(simulator, world));
            simulator.Add(new DropdownMenusSystem(simulator, world));
            simulator.Add(new UpdateDropShadowTransformSystem(simulator, world));
            RenderEnginePlugin.Create(world, SortUIObjects.Function);
        }

        public void Dispose()
        {
            simulator.Remove<UpdateDropShadowTransformSystem>();
            simulator.Remove<DropdownMenusSystem>();
            simulator.Remove<TextFieldEditingSystem>();
            simulator.Remove<ScrollViewSystem>();
            simulator.Remove<ScrollHandleMovingSystem>();
            simulator.Remove<ToggleSystem>();
            simulator.Remove<PointerDraggingSelectableSystem>();
            simulator.Remove<ComponentMixingSystem>();
            simulator.Remove<AutomationParameterSystem>();
            simulator.Remove<InvokeTriggersSystem>();
            simulator.Remove<VirtualWindowsScrollViewSystem>();
            simulator.Remove<LabelTextSystem>();
            simulator.Remove<ResizingSystem>();
            simulator.Remove<SelectionSystem>();
            simulator.Remove<CanvasSystem>();
        }
    }
}