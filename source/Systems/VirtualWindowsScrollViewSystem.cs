using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using UI.Components;
using Worlds;

namespace UI.Systems
{
    public class VirtualWindowsScrollViewSystem : ISystem
    {
        void ISystem.Update(Simulator simulator, double deltaTime)
        {
            World world = simulator.world;
            int ltwType = world.Schema.GetComponentType<LocalToWorld>();

            ComponentQuery<IsVirtualWindow> query = new(world);
            query.ExcludeDisabled(true);
            foreach (var v in query)
            {
                uint virtualWindowEntity = v.entity;
                ref IsVirtualWindow component = ref v.component1;
                VirtualWindow virtualWindow = new Entity(world, virtualWindowEntity).As<VirtualWindow>();
                rint scrollBarReference = component.scrollBarReference;
                rint viewReference = component.viewReference;
                uint scrollBarEntity = world.GetReference(virtualWindowEntity, scrollBarReference);
                uint viewEntity = world.GetReference(virtualWindowEntity, viewReference);
                ScrollBar scrollBar = new Entity(world, scrollBarEntity).As<ScrollBar>();
                View view = new Entity(world, viewEntity).As<View>();
                Entity content = view.Content;
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                int childCount = content.ChildCount;
                if (childCount > 0)
                {
                    (minY, maxY) = GetMinMax(world, content.value, ltwType, childCount);
                }

                Vector2 windowSize = virtualWindow.Size;
                Vector2 axis = scrollBar.Axis;
                if (axis.X > axis.Y)
                {
                    //horizontal
                    float handlePercentageSize = windowSize.X / (maxY - minY);
                    if (float.IsNaN(handlePercentageSize) || float.IsInfinity(handlePercentageSize))
                    {
                        handlePercentageSize = 1f;
                    }

                    view.ContentSize = new(maxY - minY, windowSize.Y);
                    scrollBar.HandlePercentageSize = handlePercentageSize;
                }
                else if (axis.Y > axis.X)
                {
                    //vertical
                    float handlePercentageSize = windowSize.Y / (maxY - minY);
                    if (float.IsNaN(handlePercentageSize) || float.IsInfinity(handlePercentageSize))
                    {
                        handlePercentageSize = 1f;
                    }

                    view.ContentSize = new(windowSize.X, maxY - minY);
                    scrollBar.HandlePercentageSize = handlePercentageSize;
                }
                else
                {
                    //both
                }
            }
        }

        private static (float min, float max) GetMinMax(World world, uint entity, int ltwType, int childCount)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            Span<uint> children = stackalloc uint[childCount];
            world.CopyChildrenTo(entity, children);
            for (int i = 0; i < children.Length; i++)
            {
                uint child = children[i];
                ref LocalToWorld childLtw = ref world.TryGetComponent<LocalToWorld>(child, ltwType, out bool contains);
                if (contains)
                {
                    float y = childLtw.Position.Y;
                    if (y < min)
                    {
                        min = y;
                    }

                    if (y > max)
                    {
                        max = y;
                    }
                }
            }

            return (min, max);
        }
    }
}