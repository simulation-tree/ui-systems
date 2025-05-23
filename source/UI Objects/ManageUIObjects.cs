using Collections.Generic;
using Data.Messages;
using Serialization.JSON;
using Simulation;
using System;
using UI.Components;
using Unmanaged;
using Worlds;

namespace UI.Systems
{
    public class ManageUIObjects : ISystem, IDisposable
    {
        private readonly Operation operation;
        private readonly Dictionary<Entity, uint> sceneObjects;

        public ManageUIObjects()
        {
            operation = new();
            sceneObjects = new();
        }

        public void Dispose()
        {
            sceneObjects.Dispose();
            operation.Dispose();
        }

        void ISystem.Update(Simulator simulator, double deltaTime)
        {
            CreateObjects(simulator, operation, sceneObjects, deltaTime);

            if (operation.Count > 0)
            {
                operation.Perform(simulator.world);
                operation.Reset();
            }
        }

        private static void CreateObjects(Simulator simulator, Operation operation, Dictionary<Entity, uint> sceneObjects, double deltaTime)
        {
            World world = simulator.world;
            Schema schema = world.Schema;
            int uiObjectRequestComponent = schema.GetComponentType<IsUIObjectRequest>();
            int uiObjectTag = schema.GetTagType<IsUIObject>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (!definition.ContainsTag(uiObjectTag) && definition.ContainsComponent(uiObjectRequestComponent))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsUIObjectRequest> requestComponents = chunk.GetComponents<IsUIObjectRequest>(uiObjectRequestComponent);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsUIObjectRequest request = ref requestComponents[i];
                        if (request.status == IsUIObjectRequest.Status.Submitted)
                        {
                            request.status = IsUIObjectRequest.Status.Loading;
                        }

                        if (request.status == IsUIObjectRequest.Status.Loading)
                        {
                            if (TryImportUIObject(simulator, request.address, sceneObjects, operation))
                            {
                                //yay
                            }
                            else
                            {
                                request.duration += deltaTime;
                                if (request.duration >= request.timeout)
                                {
                                    request.status = IsUIObjectRequest.Status.NotFound;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool TryImportUIObject(Simulator simulator, ASCIIText256 address, Dictionary<Entity, uint> sceneObjects, Operation operation)
        {
            LoadData message = new(simulator.world, address);
            simulator.Broadcast(ref message);
            if (message.TryConsume(out ByteReader data))
            {
                JSONReader jsonReader = new(data);
                data.Dispose();
                return true;
            }

            return false;
        }
    }
}