using Rendering;
using Simulation;
using System;
using UI.Systems.RenderEnginePlugins;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct RegisterUISortPlugin : ISystem
    {
        readonly void IDisposable.Dispose()
        {
        }

        readonly void ISystem.Start(in SystemContext context, in World world)
        {
            if (context.IsSimulatorWorld(world))
            {
                RenderEnginePlugin.Create(world, SortUIObjects.Function);
            }
        }

        readonly void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
        }

        readonly void ISystem.Finish(in SystemContext context, in World world)
        {
        }
    }
}