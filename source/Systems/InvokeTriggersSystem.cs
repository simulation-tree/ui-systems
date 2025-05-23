using Collections.Generic;
using Simulation;
using System;
using UI.Components;
using Worlds;

namespace UI.Systems
{
    public class InvokeTriggersSystem : ISystem, IDisposable
    {
        private readonly Array<Entity> currentEntities;
        private readonly Dictionary<int, List<Entity>> entitiesPerTrigger;
        private readonly Dictionary<int, IsTrigger> functions;

        public InvokeTriggersSystem()
        {
            currentEntities = new(4);
            entitiesPerTrigger = new(4);
            functions = new(4);
        }

        public void Dispose()
        {
            foreach (int functionHash in entitiesPerTrigger.Keys)
            {
                entitiesPerTrigger[functionHash].Dispose();
            }

            functions.Dispose();
            entitiesPerTrigger.Dispose();
            currentEntities.Dispose();
        }

        void ISystem.Update(Simulator simulator, double deltaTime)
        {
            //find new entities
            World world = simulator.world;
            int triggerComponent = world.Schema.GetComponentType<IsTrigger>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(triggerComponent) && !definition.ContainsTag(Schema.DisabledTagType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsTrigger> triggers = chunk.GetComponents<IsTrigger>(triggerComponent);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsTrigger trigger = ref triggers[i];
                        int triggerHash = trigger.GetHashCode();
                        if (!entitiesPerTrigger.TryGetValue(triggerHash, out List<Entity> entitiesList))
                        {
                            entitiesList = new();
                            entitiesPerTrigger.Add(triggerHash, entitiesList);
                            functions.Add(triggerHash, trigger);
                        }

                        Entity entityContainer = new(world, entities[i]);
                        entitiesList.Add(entityContainer);
                    }
                }
            }

            foreach (int functionHash in entitiesPerTrigger.Keys)
            {
                IsTrigger trigger = functions[functionHash];
                List<Entity> entities = entitiesPerTrigger[functionHash];

                //remove entities that no longer exist
                for (int i = entities.Count - 1; i >= 0; i--)
                {
                    Entity entity = entities[i];
                    if (entity.IsDestroyed)
                    {
                        entities.RemoveAt(i);
                    }
                }

                currentEntities.Length = entities.Count;
                currentEntities.CopyFrom(entities.AsSpan());
                trigger.filter.Invoke(currentEntities.AsSpan(), trigger.userData);
                for (int i = 0; i < currentEntities.Length; i++)
                {
                    Entity entity = currentEntities[i];
                    if (entity != default && trigger.callback != default)
                    {
                        trigger.callback.Invoke(entity);
                    }
                }

                entities.Clear();
            }
        }
    }
}