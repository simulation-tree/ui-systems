using Collections.Generic;
using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using UI.Components;
using Worlds;

namespace UI.Systems
{
    public partial struct ScrollHandleMovingSystem : ISystem
    {
        private readonly List<uint> scrollHandleEntities;
        private readonly List<uint> scrollRegionEntities;
        private uint currentScrollHandleEntity;
        private uint currentPointer;
        private Vector2 dragOffset;

        public ScrollHandleMovingSystem()
        {
            scrollHandleEntities = new(16);
            scrollRegionEntities = new(16);
            currentScrollHandleEntity = default;
            currentPointer = default;
            dragOffset = default;
        }

        public readonly void Dispose()
        {
            scrollRegionEntities.Dispose();
            scrollHandleEntities.Dispose();
        }

        readonly void ISystem.Start(in SystemContext context, in World world)
        {
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            ComponentQuery<IsScrollBar> scrollBarQuery = new(world);
            foreach (var s in scrollBarQuery)
            {
                uint scrollBarEntity = s.entity;
                rint scrollHandleReference = s.component1.scrollHandleReference;
                uint scrollHandleEntity = world.GetReference(scrollBarEntity, scrollHandleReference);
                uint scrollRegionEntity = world.GetParent(scrollHandleEntity);
                scrollHandleEntities.Add(scrollHandleEntity);
                scrollRegionEntities.Add(scrollRegionEntity);
            }

            ComponentQuery<IsPointer> pointerQuery = new(world);
            foreach (var p in pointerQuery)
            {
                uint pointerEntity = p.entity;
                ref IsPointer pointer = ref p.component1;
                rint hoveringOverReference = pointer.hoveringOverReference;
                uint hoveringOverEntity = hoveringOverReference == default ? default : world.GetReference(pointerEntity, hoveringOverReference);
                Vector2 pointerPosition = pointer.position;
                Vector2 pointerScroll = pointer.scroll;
                bool pressed = pointer.HasPrimaryIntent;
                if (pressed && currentPointer == default)
                {
                    if (scrollHandleEntities.Contains(hoveringOverEntity))
                    {
                        //start dragging
                        LocalToWorld scrollHandleLtw = world.GetComponent<LocalToWorld>(hoveringOverEntity);
                        Vector3 scrollHandlePosition = scrollHandleLtw.Position;
                        Vector2 scrollBarPercentage = default;
                        scrollBarPercentage.X = pointerPosition.X - scrollHandlePosition.X;
                        scrollBarPercentage.Y = pointerPosition.Y - scrollHandlePosition.Y;
                        currentScrollHandleEntity = hoveringOverEntity;
                        currentPointer = pointerEntity;
                        dragOffset = scrollBarPercentage;
                    }
                    else if (scrollRegionEntities.Contains(hoveringOverEntity))
                    {
                        //teleport scroll value
                        uint scrollBarEntity = world.GetParent(hoveringOverEntity);
                        rint scrollHandleReference = world.GetComponent<IsScrollBar>(scrollBarEntity).scrollHandleReference;
                        uint scrollHandleEntity = world.GetReference(scrollBarEntity, scrollHandleReference);
                        Vector2 value = GetScrollBarValue(world, scrollHandleEntity, pointerPosition);
                        ref IsScrollBar component = ref world.GetComponent<IsScrollBar>(scrollBarEntity);
                        component.value = value;
                    }
                }
                else if (!pressed)
                {
                    if (scrollHandleEntities.Contains(hoveringOverEntity))
                    {
                        //move scrollbar by scrolling when hovering over the handle
                        uint scrollRegionEntity = world.GetParent(hoveringOverEntity);
                        uint scrollBarEntity = world.GetParent(scrollRegionEntity);
                        ref IsScrollBar component = ref world.GetComponent<IsScrollBar>(scrollBarEntity);
                        component.value += pointerScroll * component.axis;
                        component.value = Clamp(component.value, Vector2.Zero, Vector2.One);
                    }
                    else if (scrollRegionEntities.Contains(hoveringOverEntity))
                    {
                        //move when hovering over the region
                        uint scrollBarEntity = world.GetParent(hoveringOverEntity);
                        ref IsScrollBar component = ref world.GetComponent<IsScrollBar>(scrollBarEntity);
                        component.value += pointerScroll * component.axis;
                        component.value = Clamp(component.value, Vector2.Zero, Vector2.One);
                    }

                    currentPointer = default;
                    currentScrollHandleEntity = default;
                }
            }

            //move scrollbar value by dragging
            if (scrollHandleEntities.Contains(currentScrollHandleEntity))
            {
                IsPointer pointer = world.GetComponent<IsPointer>(currentPointer);
                Vector2 pointerPosition = pointer.position - dragOffset;
                Vector2 value = GetScrollBarValue(world, currentScrollHandleEntity, pointerPosition);

                uint scrollRegionEntity = world.GetParent(currentScrollHandleEntity);
                uint scrollBarEntity = world.GetParent(scrollRegionEntity);
                ref IsScrollBar component = ref world.GetComponent<IsScrollBar>(scrollBarEntity);
                component.value = value;
            }

            //update scroll bar visuals
            foreach (var s in scrollBarQuery)
            {
                rint scrollHandleReference = s.component1.scrollHandleReference;
                uint scrollHandleEntity = world.GetReference(s.entity, scrollHandleReference);
                Vector2 value = s.component1.value;
                UpdateScrollBarVisual(world, scrollHandleEntity, value);
            }

            scrollHandleEntities.Clear();
            scrollRegionEntities.Clear();
        }

        readonly void ISystem.Finish(in SystemContext context, in World world)
        {
        }

        private static Vector2 GetScrollBarValue(World world, uint scrollHandleEntity, Vector2 pointerPosition)
        {
            uint scrollRegionEntity = world.GetParent(scrollHandleEntity);
            uint scrollBarEntity = world.GetParent(scrollRegionEntity);
            IsScrollBar component = world.GetComponent<IsScrollBar>(scrollBarEntity);
            LocalToWorld scrollHandleLtw = world.GetComponent<LocalToWorld>(scrollHandleEntity);
            Vector3 scrollHandleSize = scrollHandleLtw.Scale;
            Vector2 axis = component.axis;

            LocalToWorld scrollRegionLtw = world.GetComponent<LocalToWorld>(scrollRegionEntity);
            Vector3 scrollRegionSize = scrollRegionLtw.Scale;
            Vector3 scrollRegionPosition = scrollRegionLtw.Position;
            Vector2 scrollRegionMin = new(scrollRegionPosition.X, scrollRegionPosition.Y);
            Vector2 scrollRegionMax = scrollRegionMin + new Vector2(scrollRegionSize.X, scrollRegionSize.Y);
            scrollRegionMax.X -= scrollHandleSize.X * axis.X;
            scrollRegionMax.Y -= scrollHandleSize.Y * axis.Y;

            pointerPosition = Clamp(pointerPosition, scrollRegionMin, scrollRegionMax);
            Vector2 scrollRegionPercentage = default;
            scrollRegionPercentage.X = (pointerPosition.X - scrollRegionMin.X) / (scrollRegionMax.X - scrollRegionMin.X);
            scrollRegionPercentage.Y = (pointerPosition.Y - scrollRegionMin.Y) / (scrollRegionMax.Y - scrollRegionMin.Y);

            Vector2 value = default;
            value.X = scrollRegionPercentage.X * axis.X;
            value.Y = scrollRegionPercentage.Y * axis.Y;
            return value;
        }

        private static void UpdateScrollBarVisual(World world, uint scrollHandleEntity, Vector2 value)
        {
            uint scrollRegionEntity = world.GetParent(scrollHandleEntity);
            uint scrollBarEntity = world.GetParent(scrollRegionEntity);
            Vector2 axis = world.GetComponent<IsScrollBar>(scrollBarEntity).axis;

            LocalToWorld scrollBarLtw = world.GetComponent<LocalToWorld>(scrollHandleEntity);
            LocalToWorld scrollRegionLtw = world.GetComponent<LocalToWorld>(scrollRegionEntity);
            Vector3 scrollBarSize = scrollBarLtw.Scale;
            Vector3 scrollRegionSize = scrollRegionLtw.Scale;

            value.X *= 1 - ((scrollBarSize.X / scrollRegionSize.X) * axis.X);
            value.Y *= 1 - ((scrollBarSize.Y / scrollRegionSize.Y) * axis.Y);

            ref Position position = ref world.GetComponent<Position>(scrollHandleEntity);
            position.value.X = value.X;
            position.value.Y = value.Y;
        }

        private static Vector2 Clamp(Vector2 input, Vector2 min, Vector2 max)
        {
            if (input.X < min.X)
            {
                input.X = min.X;
            }
            else if (input.X > max.X)
            {
                input.X = max.X;
            }

            if (input.Y < min.Y)
            {
                input.Y = min.Y;
            }
            else if (input.Y > max.Y)
            {
                input.Y = max.Y;
            }

            return input;
        }
    }
}