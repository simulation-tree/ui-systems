using Collections;
using UI.Components;
using Simulation;
using System;
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
            ComponentQuery<IsTrigger> invokeQuery = new(world);
            invokeQuery.ExcludeDisabled(true);
            foreach (var x in invokeQuery)
            {
                Entity entity = new(world, x.entity);
                ref IsTrigger trigger = ref x.component1;
                int triggerHash = trigger.GetHashCode();
                if (!entitiesPerTrigger.TryGetValue(triggerHash, out List<Entity> entities))
                {
                    entities = new();
                    entitiesPerTrigger.Add(triggerHash, entities);
                    functions.Add(triggerHash, trigger);
                }

                entities.Add(entity);
            }

            foreach (int functionHash in entitiesPerTrigger.Keys)
            {
                IsTrigger trigger = functions[functionHash];
                List<Entity> entities = entitiesPerTrigger[functionHash];

                //remove entities that no longer exist
                for (uint i = entities.Count - 1; i != uint.MaxValue; i--)
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
                for (uint i = 0; i < currentEntities.Length; i++)
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