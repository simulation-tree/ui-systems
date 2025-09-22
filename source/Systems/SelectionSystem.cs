using Collections.Generic;
using Rendering;
using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using UI.Components;
using UI.Messages;
using Worlds;

namespace UI.Systems
{
    public partial class SelectionSystem : SystemBase, IListener<UIUpdate>
    {
        private readonly World world;
        private readonly Dictionary<uint, PointerAction> pointerStates;
        private readonly List<(uint entity, LocalToWorld ltw)> selectableEntities;
        private readonly int pointerType;
        private readonly int selectableType;
        private readonly int ltwType;
        private readonly int worldRotationType;
        private readonly BitMask selectableComponents;

        public SelectionSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            pointerStates = new(4);
            selectableEntities = new(4);

            Schema schema = world.Schema;
            pointerType = schema.GetComponentType<IsPointer>();
            selectableType = schema.GetComponentType<IsSelectable>();
            ltwType = schema.GetComponentType<LocalToWorld>();
            worldRotationType = schema.GetComponentType<WorldRotation>();
            selectableComponents = new(selectableType, ltwType);
        }

        public override void Dispose()
        {
            selectableEntities.Dispose();
            pointerStates.Dispose();
        }

        void IListener<UIUpdate>.Receive(ref UIUpdate message)
        {
            Span<(uint entity, LocalToWorld ltw)> selectableEntitiesSpan = selectableEntities.AsSpan();
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.ComponentTypes.Contains(pointerType) && !chunk.IsDisabled)
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsPointer> components = chunk.GetComponents<IsPointer>(pointerType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsPointer component = ref components[i];
                        uint pointer = entities[i];
                        Vector2 pointerPosition = component.position;
                        float lastDepth = float.MinValue;
                        uint newHoveringOver = default;
                        bool hasPrimaryIntent = component.HasPrimaryIntent;
                        bool hasSecondaryIntent = component.HasSecondaryIntent;
                        bool primaryIntentStarted = false;
                        bool secondaryIntentStarted = false;
                        if (pointerStates.TryAdd(pointer, component.action))
                        {
                            primaryIntentStarted = hasPrimaryIntent;
                            secondaryIntentStarted = hasSecondaryIntent;
                        }
                        else
                        {
                            ref PointerAction action = ref pointerStates[pointer];
                            bool lastPrimaryIntent = (action & PointerAction.Primary) != 0;
                            if (!lastPrimaryIntent && hasPrimaryIntent)
                            {
                                primaryIntentStarted = true;
                            }

                            bool lastSecondaryIntent = (action & PointerAction.Secondary) != 0;
                            if (!lastSecondaryIntent && hasSecondaryIntent)
                            {
                                secondaryIntentStarted = true;
                            }

                            action = component.action;
                        }

                        //find currently hovering over entity
                        FindSelectableEntities(world, component.selectionMask);
                        foreach ((uint selectableEntity, LocalToWorld ltw) in selectableEntitiesSpan)
                        {
                            Vector3 position = ltw.Position;
                            Vector3 scale = ltw.Scale;
                            ref WorldRotation worldRotationComponent = ref world.TryGetComponent<WorldRotation>(selectableEntity, worldRotationType, out bool hasWorldRotation);
                            if (hasWorldRotation)
                            {
                                scale = Vector3.Transform(scale, worldRotationComponent.value);
                            }

                            Vector2 offset = new Vector2(position.X, position.Y) + new Vector2(scale.X, scale.Y);
                            Vector2 min = Vector2.Min(new(position.X, position.Y), offset);
                            Vector2 max = Vector2.Max(new(position.X, position.Y), offset);
                            UIBounds bounds = new(min, max);
                            if (bounds.Contains(pointerPosition))
                            {
                                float depth = position.Z;
                                if (lastDepth < depth)
                                {
                                    lastDepth = depth;
                                    newHoveringOver = selectableEntity;
                                }
                            }
                        }

                        //update currently selected entity
                        ref rint hoveringOverReference = ref component.hoveringOverReference;
                        uint currentHoveringOver = hoveringOverReference == default ? default : world.GetReference(pointer, hoveringOverReference);

                        //handle state mismatch
                        if (!world.ContainsEntity(currentHoveringOver))
                        {
                            currentHoveringOver = default;
                        }

                        if (currentHoveringOver != newHoveringOver)
                        {
                            if (currentHoveringOver != default && world.ContainsEntity(currentHoveringOver))
                            {
                                ref IsSelectable oldSelectable = ref world.TryGetComponent<IsSelectable>(currentHoveringOver, selectableType, out bool contains);
                                if (contains)
                                {
                                    oldSelectable.state = default;
                                }
                            }

                            if (newHoveringOver != default)
                            {
                                ref IsSelectable newSelectable = ref world.GetComponent<IsSelectable>(newHoveringOver, selectableType);
                                newSelectable.state |= IsSelectable.State.IsSelected;
                            }

                            if (newHoveringOver == default)
                            {
                                world.RemoveReference(pointer, hoveringOverReference);
                                hoveringOverReference = default;
                            }
                            else
                            {
                                if (hoveringOverReference == default)
                                {
                                    hoveringOverReference = world.AddReference(pointer, newHoveringOver);
                                }
                                else
                                {
                                    world.SetReference(pointer, hoveringOverReference, newHoveringOver);
                                }
                            }
                        }
                        else if (newHoveringOver != default && world.ContainsEntity(newHoveringOver))
                        {
                            ref IsSelectable selectable = ref world.GetComponent<IsSelectable>(newHoveringOver, selectableType);
                            if (primaryIntentStarted)
                            {
                                selectable.state |= IsSelectable.State.WasPrimaryInteractedWith;
                            }
                            else if (hasPrimaryIntent)
                            {
                                if (selectable.WasPrimaryInteractedWith)
                                {
                                    selectable.state |= IsSelectable.State.IsPrimaryInteractedWith;
                                }

                                selectable.state &= ~IsSelectable.State.WasPrimaryInteractedWith;
                            }
                            else
                            {
                                selectable.state &= ~IsSelectable.State.IsPrimaryInteractedWith;
                                selectable.state &= ~IsSelectable.State.WasPrimaryInteractedWith;
                            }

                            if (secondaryIntentStarted)
                            {
                                selectable.state |= IsSelectable.State.WasSecondaryInteractedWith;
                            }
                            else if (hasSecondaryIntent)
                            {
                                if (selectable.WasSecondaryInteractedWith)
                                {
                                    selectable.state |= IsSelectable.State.IsSecondaryInteractedWith;
                                }

                                selectable.state &= ~IsSelectable.State.WasSecondaryInteractedWith;
                            }
                            else
                            {
                                selectable.state &= ~IsSelectable.State.IsSecondaryInteractedWith;
                                selectable.state &= ~IsSelectable.State.WasSecondaryInteractedWith;
                            }
                        }
                    }
                }
            }

            //todo: handle pressing Tab to switch to the next selectable
            //todo: handle using arrow keys to switch to an adjacent selectable
        }

        /// <summary>
        /// Finds all renderers that have a selection mask
        /// which intersects with the given <paramref name="selectionMask"/>.
        /// </summary>
        private void FindSelectableEntities(World world, LayerMask selectionMask)
        {
            selectableEntities.Clear();

            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.ComponentTypes.ContainsAll(selectableComponents) && !chunk.IsDisabled)
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsSelectable> selectableComponents = chunk.GetComponents<IsSelectable>(selectableType);
                    ComponentEnumerator<LocalToWorld> ltwComponents = chunk.GetComponents<LocalToWorld>(ltwType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsSelectable selectable = ref selectableComponents[i];
                        if (selectionMask.ContainsAny(selectable.selectionMask))
                        {
                            uint entity = entities[i];
                            selectableEntities.Add((entity, ltwComponents[i]));
                        }
                    }
                }
            }
        }
    }
}