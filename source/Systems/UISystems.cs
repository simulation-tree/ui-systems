using Rendering;
using Simulation;
using System;
using UI.Systems.RenderEnginePlugins;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct UISystems : ISystem
    {
        readonly void IDisposable.Dispose()
        {
        }

        readonly void ISystem.Start(in SystemContext context, in World world)
        {
            if (context.IsSimulatorWorld(world))
            {
                context.AddSystem(new CanvasSystem());
                context.AddSystem(new SelectionSystem());
                context.AddSystem(new ResizingSystem());
                context.AddSystem(new LabelTextSystem());
                context.AddSystem(new VirtualWindowsScrollViewSystem());
                context.AddSystem(new InvokeTriggersSystem());
                context.AddSystem(new AutomationParameterSystem());
                context.AddSystem(new ComponentMixingSystem());
                context.AddSystem(new PointerDraggingSelectableSystem());
                context.AddSystem(new ToggleSystem());
                context.AddSystem(new ScrollHandleMovingSystem());
                context.AddSystem(new ScrollViewSystem());
                context.AddSystem(new TextFieldEditingSystem());
                context.AddSystem(new DropdownMenusSystem());
                context.AddSystem(new UpdateDropShadowTransformSystem());

                RenderEnginePlugin.Create(world, SortUIObjects.Function);
            }
        }

        readonly void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
        }

        readonly void ISystem.Finish(in SystemContext context, in World world)
        {
            if (context.IsSimulatorWorld(world))
            {
                context.RemoveSystem<UpdateDropShadowTransformSystem>();
                context.RemoveSystem<DropdownMenusSystem>();
                context.RemoveSystem<TextFieldEditingSystem>();
                context.RemoveSystem<ScrollViewSystem>();
                context.RemoveSystem<ScrollHandleMovingSystem>();
                context.RemoveSystem<ToggleSystem>();
                context.RemoveSystem<PointerDraggingSelectableSystem>();
                context.RemoveSystem<ComponentMixingSystem>();
                context.RemoveSystem<AutomationParameterSystem>();
                context.RemoveSystem<InvokeTriggersSystem>();
                context.RemoveSystem<VirtualWindowsScrollViewSystem>();
                context.RemoveSystem<LabelTextSystem>();
                context.RemoveSystem<ResizingSystem>();
                context.RemoveSystem<SelectionSystem>();
                context.RemoveSystem<CanvasSystem>();
            }
        }
    }
}