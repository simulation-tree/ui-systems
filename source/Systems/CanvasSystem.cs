using Cameras;
using Rendering;
using Simulation;
using System;
using System.Numerics;
using Transforms.Components;
using UI.Components;
using Unmanaged;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct CanvasSystem : ISystem
    {
        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            USpan<uint> destroyedCanvases = stackalloc uint[64];
            uint destroyedCanvasCount = 0;

            ComponentType canvasType = world.Schema.GetComponent<IsCanvas>();
            ComponentType positionType = world.Schema.GetComponent<Position>();
            ComponentType scaleType = world.Schema.GetComponent<Scale>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.Contains(canvasType) && definition.Contains(positionType) && definition.Contains(scaleType) && !definition.Contains(TagType.Disabled))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<IsCanvas> canvasComponents = chunk.GetComponents<IsCanvas>(canvasType);
                    USpan<Position> positionComponents = chunk.GetComponents<Position>(positionType);
                    USpan<Scale> scaleComponents = chunk.GetComponents<Scale>(scaleType);
                    for (uint i = 0; i < entities.Length; i++)
                    {
                        uint canvasEntity = entities[i];
                        ref IsCanvas component = ref canvasComponents[i];
                        rint cameraReference = component.cameraReference;
                        uint cameraEntity = world.GetReference(canvasEntity, cameraReference);
                        float distanceFromCamera = Settings.ZScale;
                        Vector2 size = default;
                        if (cameraEntity != default && world.ContainsEntity(cameraEntity))
                        {
                            Camera camera = new Entity(world, cameraEntity).As<Camera>();
                            if (camera.IsDestroyed || !camera.IsCompliant)
                            {
                                //todo: the check for whether the camera entity is itself a camera, shouldnt be necessary
                                //without it it sometimes fails, other times doesnt with the multiple windows program, not sure why
                                destroyedCanvases[destroyedCanvasCount++] = canvasEntity;
                                continue;
                            }

                            Destination destination = camera.Destination;
                            if (destination != default && !destination.IsDestroyed)
                            {
                                size = destination.SizeAsVector2;
                            }

                            distanceFromCamera += camera.Depth.min;
                        }

                        ref Position position = ref positionComponents[i];
                        position.value.Z = distanceFromCamera;

                        ref Scale scale = ref scaleComponents[i];
                        scale.value.X = size.X;
                        scale.value.Y = size.Y;
                    }
                }
            }

            for (uint i = 0; i < destroyedCanvasCount; i++)
            {
                world.DestroyEntity(destroyedCanvases[i]);
            }
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
        }
    }
}