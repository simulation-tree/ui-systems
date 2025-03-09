using Collections;
using Collections.Generic;
using Rendering;
using Rendering.Components;
using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using UI.Components;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct ScrollViewSystem : ISystem
    {
        private readonly List<uint> scrollBarLinkEntities;

        private ScrollViewSystem(List<uint> scrollBarLinkEntities)
        {
            this.scrollBarLinkEntities = scrollBarLinkEntities;
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                List<uint> scrollBarLinkEntities = new();
                systemContainer.Write(new ScrollViewSystem(scrollBarLinkEntities));
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            Update(world);
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                scrollBarLinkEntities.Dispose();
            }
        }

        private readonly void Update(World world)
        {
            FindScrollBarLinkEntities(world);

            ComponentQuery<IsView, LocalToWorld> viewQuery = new(world);
            foreach (var v in viewQuery)
            {
                uint scrollViewEntity = v.entity;
                ref LocalToWorld ltw = ref v.component2;
                ref IsView view = ref v.component1;
                rint contentReference = view.contentReference;
                uint contentEntity = world.GetReference(scrollViewEntity, contentReference);
                Vector3 viewPosition = ltw.Position;
                Vector3 viewScale = ltw.Scale;
                Destination destination = new Entity(world, scrollViewEntity).GetCanvas().Camera.Destination;
                if (destination == default || destination.IsDestroyed)
                {
                    continue;
                }

                LocalToWorld contentLtw = world.GetComponent<LocalToWorld>(contentEntity);
                Vector3 contentScale = contentLtw.Scale;
                if (scrollBarLinkEntities.Contains(scrollViewEntity))
                {
                    ViewScrollBarLink scrollBarLink = world.GetComponent<ViewScrollBarLink>(scrollViewEntity);
                    rint scrollBarReference = scrollBarLink.scrollBarReference;
                    uint scrollBarEntity = world.GetReference(scrollViewEntity, scrollBarReference);
                    ref IsScrollBar scrollBar = ref world.GetComponent<IsScrollBar>(scrollBarEntity);

                    //let points scroll the bar
                    ComponentQuery<IsPointer> pointerQuery = new(world);
                    foreach (var p in pointerQuery)
                    {
                        ref IsPointer pointer = ref p.component1;
                        Vector2 pointerPosition = pointer.position;
                        bool hoveredOver = pointerPosition.X >= viewPosition.X && pointerPosition.X <= viewPosition.X + viewScale.X && pointerPosition.Y >= viewPosition.Y && pointerPosition.Y <= viewPosition.Y + viewScale.Y;
                        if (hoveredOver)
                        {
                            scrollBar.value += pointer.scroll * scrollBar.axis;
                            if (scrollBar.value.X < 0)
                            {
                                scrollBar.value.X = 0;
                            }

                            if (scrollBar.value.Y < 0)
                            {
                                scrollBar.value.Y = 0;
                            }

                            if (scrollBar.value.X > 1)
                            {
                                scrollBar.value.X = 1;
                            }

                            if (scrollBar.value.Y > 1)
                            {
                                scrollBar.value.Y = 1;
                            }
                        }
                    }

                    Vector2 value = scrollBar.value;
                    value.X *= (contentScale.X - viewScale.X);
                    value.Y *= (contentScale.Y - viewScale.Y);
                    view.value = value;
                }

                (uint width, uint height) destinationSize = destination.Size;
                Vector4 region = new(0, 0, 1, 1);
                region.X = viewPosition.X;
                region.Y = destinationSize.height - (viewPosition.Y + viewScale.Y);
                region.Z = viewScale.X;
                region.W = viewScale.Y;
                if (region.X < 0)
                {
                    region.X = 0;
                }

                if (region.Y < 0)
                {
                    region.Y = 0;
                }

                if (region.Z > destinationSize.width)
                {
                    region.Z = destinationSize.width;
                }

                if (region.W > destinationSize.height)
                {
                    region.W = destinationSize.height;
                }

                //UpdateScissors(world, contentEntity, region);
                //update scissor only for content entity
                ref RendererScissor scissor = ref world.TryGetComponent<RendererScissor>(contentEntity, out bool contains);
                if (!contains)
                {
                    world.AddComponent(contentEntity, new RendererScissor(region));
                }
                else
                {
                    scissor.value = region;
                }

                Vector2 scrollValue = view.value;
                if (float.IsNaN(scrollValue.X))
                {
                    scrollValue.X = 0f;
                }

                if (float.IsNaN(scrollValue.Y))
                {
                    scrollValue.Y = 0f;
                }

                ref Position position = ref world.GetComponent<Position>(contentEntity);
                position.value.X = 1 - scrollValue.X;
                position.value.Y = 1 - scrollValue.Y;
            }
        }

        private readonly void FindScrollBarLinkEntities(World world)
        {
            scrollBarLinkEntities.Clear();
            ComponentQuery<ViewScrollBarLink> query = new(world);
            foreach (var q in query)
            {
                scrollBarLinkEntities.Add(q.entity);
            }
        }

        private static void UpdateScissors(World world, uint contentEntity, Vector4 region)
        {
            ReadOnlySpan<uint> contentChildren = world.GetChildren(contentEntity);
            foreach (uint child in contentChildren)
            {
                if (world.ContainsComponent<IsRenderer>(child))
                {
                    ref RendererScissor scissor = ref world.TryGetComponent<RendererScissor>(child, out bool contains);
                    if (!contains)
                    {
                        world.AddComponent(child, new RendererScissor(region));
                    }
                    else
                    {
                        scissor.value = region;
                    }
                }

                UpdateScissors(world, child, region);
            }
        }
    }
}