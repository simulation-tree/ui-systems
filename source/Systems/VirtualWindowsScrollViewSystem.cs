using UI.Components;
using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using Unmanaged;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct VirtualWindowsScrollViewSystem : ISystem
    {
        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            Update(world);
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
        }

        private readonly void Update(World world)
        {
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
                USpan<uint> children = content.Children;
                for (uint i = 0; i < children.Length; i++)
                {
                    Entity child = new(world, children[i]);
                    ref LocalToWorld childLtw = ref child.TryGetComponent<LocalToWorld>(out bool contains);
                    if (contains)
                    {
                        float y = childLtw.Position.Y;
                        if (y < minY)
                        {
                            minY = y;
                        }

                        if (y > maxY)
                        {
                            maxY = y;
                        }
                    }
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
    }
}