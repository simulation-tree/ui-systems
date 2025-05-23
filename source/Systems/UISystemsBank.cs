using Rendering;
using Simulation;
using System;
using UI.Systems.RenderEnginePlugins;

namespace UI.Systems
{
    public class UISystemsBank : IDisposable
    {
        public readonly Simulator simulator;

        public UISystemsBank(Simulator simulator)
        {
            this.simulator = simulator;
            simulator.Add(new CanvasSystem());
            simulator.Add(new SelectionSystem());
            simulator.Add(new ResizingSystem());
            simulator.Add(new LabelTextSystem());
            simulator.Add(new VirtualWindowsScrollViewSystem());
            simulator.Add(new InvokeTriggersSystem());
            simulator.Add(new AutomationParameterSystem());
            simulator.Add(new ComponentMixingSystem());
            simulator.Add(new PointerDraggingSelectableSystem());
            simulator.Add(new ToggleSystem());
            simulator.Add(new ScrollHandleMovingSystem());
            simulator.Add(new ScrollViewSystem());
            simulator.Add(new TextFieldEditingSystem());
            simulator.Add(new DropdownMenusSystem());
            simulator.Add(new UpdateDropShadowTransformSystem());
            RenderEnginePlugin.Create(simulator.world, SortUIObjects.Function);
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