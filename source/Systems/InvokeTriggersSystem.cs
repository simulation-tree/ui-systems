using Collections.Generic;
using Simulation;
using System;
using UI.Components;
using UI.Messages;
using Worlds;

namespace UI.Systems
{
    //todo: this could also be part of the automations project
    public partial class InvokeTriggersSystem : SystemBase, IListener<UIUpdate>
    {
        private readonly World world;
        private readonly Array<uint> currentEntities;
        private readonly Dictionary<int, List<uint>> entitiesPerTrigger;
        private readonly Dictionary<int, IsTrigger> functions;
        private readonly int triggerType;

        public InvokeTriggersSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            currentEntities = new(4);
            entitiesPerTrigger = new(4);
            functions = new(4);

            Schema schema = world.Schema;
            triggerType = schema.GetComponentType<IsTrigger>();
        }

        public override void Dispose()
        {
            foreach (List<uint> entityList in entitiesPerTrigger.Values)
            {
                entityList.Dispose();
            }

            functions.Dispose();
            entitiesPerTrigger.Dispose();
            currentEntities.Dispose();
        }

        void IListener<UIUpdate>.Receive(ref UIUpdate message)
        {
            //find new entities
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(triggerType) && !definition.IsDisabled)
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsTrigger> triggers = chunk.GetComponents<IsTrigger>(triggerType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsTrigger trigger = ref triggers[i];
                        int triggerHash = trigger.GetHashCode();
                        if (!entitiesPerTrigger.TryGetValue(triggerHash, out List<uint> entitiesList))
                        {
                            entitiesList = new();
                            entitiesPerTrigger.Add(triggerHash, entitiesList);
                            functions.Add(triggerHash, trigger);
                        }

                        entitiesList.Add(entities[i]);
                    }
                }
            }

            foreach (int functionHash in entitiesPerTrigger.Keys)
            {
                IsTrigger trigger = functions[functionHash];
                List<uint> entities = entitiesPerTrigger[functionHash];
                Span<uint> entitiesSpan = entities.AsSpan();

                //remove entities that no longer exist
                for (int i = entitiesSpan.Length - 1; i >= 0; i--)
                {
                    uint entity = entitiesSpan[i];
                    if (!world.ContainsEntity(entity))
                    {
                        entities.RemoveAt(i);
                    }
                }

                entitiesSpan = entities.AsSpan();
                currentEntities.Length = entitiesSpan.Length;
                currentEntities.CopyFrom(entitiesSpan);
                entities.Clear();

                Span<uint> currentEntitiesSpan = currentEntities.AsSpan();
                trigger.filter.Invoke(world, currentEntitiesSpan, trigger.userData);
                for (int i = 0; i < currentEntitiesSpan.Length; i++)
                {
                    uint entity = currentEntitiesSpan[i];
                    if (entity != default && trigger.callback != default)
                    {
                        trigger.callback.Invoke(world, entity);
                    }
                }
            }
        }
    }
}