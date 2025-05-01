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
    public readonly partial struct ManageUIObjects : ISystem
    {
        private readonly Dictionary<World, Operation> operations;
        private readonly Dictionary<Entity, uint> sceneObjects;

        public ManageUIObjects()
        {
            operations = new();
            sceneObjects = new();
        }

        readonly void IDisposable.Dispose()
        {
            sceneObjects.Dispose();

            foreach (Operation operation in operations.Values)
            {
                operation.Dispose();
            }

            operations.Dispose();
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
            ref Operation operation = ref operations.TryGetValue(world, out bool contains);
            if (!contains)
            {
                operation = ref operations.Add(world);
                operation = new();
            }
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            ref Operation operation = ref operations[world];
            CreateObjects(context, world, operation, sceneObjects, delta);

            if (operation.Count > 0)
            {
                operation.Perform(world);
                operation.Reset();
            }
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
        }

        private static void CreateObjects(SystemContext context, World world, Operation operation, Dictionary<Entity, uint> sceneObjects, TimeSpan delta)
        {
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
                            if (TryImportUIObject(context, world, request.address, sceneObjects, operation))
                            {
                                //yay
                            }
                            else
                            {
                                request.duration += delta;
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

        private static bool TryImportUIObject(SystemContext context, World world, ASCIIText256 address, Dictionary<Entity, uint> sceneObjects, Operation operation)
        {
            LoadData message = new(world, address);
            if (context.TryHandleMessage(ref message) != default)
            {
                if (message.TryConsume(out ByteReader data))
                {
                    JSONReader jsonReader = new(data);
                    data.Dispose();
                    return true;
                }
            }

            return false;
        }
    }
}