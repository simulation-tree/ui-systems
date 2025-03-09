using Collections.Generic;
using Simulation;
using System;
using UI.Components;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct InvokeTriggersSystem : ISystem
    {
        private readonly Array<Entity> currentEntities;
        private readonly Dictionary<int, List<Entity>> entitiesPerTrigger;
        private readonly Dictionary<int, IsTrigger> functions;

        private InvokeTriggersSystem(Array<Entity> currentEntities, Dictionary<int, List<Entity>> entitiesPerTrigger, Dictionary<int, IsTrigger> functions)
        {
            this.currentEntities = currentEntities;
            this.entitiesPerTrigger = entitiesPerTrigger;
            this.functions = functions;
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                Array<Entity> currentEntities = new();
                Dictionary<int, List<Entity>> entitiesPerTrigger = new();
                Dictionary<int, IsTrigger> functions = new();
                systemContainer.Write(new InvokeTriggersSystem(currentEntities, entitiesPerTrigger, functions));
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            //find new entities
            ComponentType triggerComponent = world.Schema.GetComponentType<IsTrigger>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(triggerComponent) && !definition.ContainsTag(TagType.Disabled))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    Span<IsTrigger> triggers = chunk.GetComponents<IsTrigger>(triggerComponent);
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

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                foreach (int functionHash in entitiesPerTrigger.Keys)
                {
                    entitiesPerTrigger[functionHash].Dispose();
                }

                functions.Dispose();
                entitiesPerTrigger.Dispose();
                currentEntities.Dispose();
            }
        }
    }
}