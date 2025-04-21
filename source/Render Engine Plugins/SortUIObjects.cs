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
        private static readonly uint[] materialEntities;
        private static int materialCount = 0;

        unsafe static SortUIObjects()
        {
            Function = new(&Invoke);
            materialEntities = new uint[32];
        }

        [UnmanagedCallersOnly]
        private static void Invoke(RenderEnginePluginFunction.Input input)
        {
            World world = input.world;
            if (world.Schema.ContainsComponentType<UISettings>())
            {
                if (world.TryGetFirst(out Settings settings))
                {
                    materialCount = settings.GetMaterials(materialEntities.AsSpan());
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

        private static int SortByZPosition((RenderEntity renderEntity, float position) a, (RenderEntity renderEntity, float position) b)
        {
            return a.position.CompareTo(b.position);
        }
    }
}