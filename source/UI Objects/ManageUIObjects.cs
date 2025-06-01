using Collections.Generic;
using Data.Messages;
using Serialization.JSON;
using Simulation;
using System;
using UI.Components;
using Unmanaged;
using Worlds;
using Worlds.Messages;

namespace UI.Systems
{
    public partial class ManageUIObjects : SystemBase, IListener<Update>
    {
        private readonly World world;
        private readonly Operation operation;
        private readonly Dictionary<Entity, uint> sceneObjects;

        public ManageUIObjects(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            operation = new(world);
            sceneObjects = new();
        }

        public override void Dispose()
        {
            sceneObjects.Dispose();
            operation.Dispose();
        }

        void IListener<Update>.Receive(ref Update message)
        {
            CreateObjects(message.deltaTime);

            if (operation.TryPerform())
            {
                operation.Reset();
            }
        }

        private void CreateObjects(double deltaTime)
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
                            if (TryImportUIObject(request.address))
                            {
                                request.status = IsUIObjectRequest.Status.Loaded;
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

        private bool TryImportUIObject(ASCIIText256 address)
        {
            LoadData message = new(address);
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