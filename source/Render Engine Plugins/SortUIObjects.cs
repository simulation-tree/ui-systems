using Rendering;
using Rendering.Functions;
using System;
using System.Runtime.InteropServices;
using Transforms.Components;
using UI.Components;
using Worlds;

namespace UI.Systems.RenderEnginePlugins
{
    /// <summary>
    /// Sorts UI objects by their position on the Z axis.
    /// </summary>
    public static class SortUIObjects
    {
        public static readonly RenderEnginePluginFunction Function;

        unsafe static SortUIObjects()
        {
            Function = new(&Invoke);
        }

        [UnmanagedCallersOnly]
        private static void Invoke(RenderEnginePluginFunction.Input input)
        {
            World world = input.world;
            if (world.Schema.ContainsComponentType<UISettings>())
            {
                if (world.TryGetFirst(out Settings settings))
                {
                    if (settings.IsUIMaterial(input.materialEntity))
                    {
                        int positionType = world.Schema.GetComponentType<Position>();
                        Span<RenderEntity> entities = input.Entities;
                        Span<(RenderEntity renderEntity, float z)> span = stackalloc (RenderEntity, float)[entities.Length];
                        for (int i = 0; i < entities.Length; i++)
                        {
                            RenderEntity renderEntity = entities[i];
                            if (world.TryGetComponent(renderEntity.entity, positionType, out Position position))
                            {
                                span[i] = (renderEntity, position.value.Z);
                            }
                            else
                            {
                                span[i] = (renderEntity, 0);
                            }
                        }

                        span.Sort(SortByZPosition);
                        for (int i = 0; i < entities.Length; i++)
                        {
                            entities[i] = span[i].renderEntity;
                        }
                    }
                }
            }
        }

        private static int SortByZPosition((RenderEntity, float z) x, (RenderEntity, float z) y)
        {
            return x.z.CompareTo(y.z);
        }
    }
}