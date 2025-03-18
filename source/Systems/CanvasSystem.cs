using Cameras;
using Rendering;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Transforms.Components;
using UI.Components;
using Worlds;

namespace UI.Systems
{
    public readonly partial struct CanvasSystem : ISystem
    {
        readonly void IDisposable.Dispose()
        {
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
        }

        [SkipLocalsInit]
        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            Span<uint> destroyedCanvases = stackalloc uint[64];
            int destroyedCanvasCount = 0;
            int canvasType = world.Schema.GetComponentType<IsCanvas>();
            int positionType = world.Schema.GetComponentType<Position>();
            int scaleType = world.Schema.GetComponentType<Scale>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(canvasType) && definition.ContainsComponent(positionType) && definition.ContainsComponent(scaleType) && !definition.ContainsTag(Schema.DisabledTagType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsCanvas> canvasComponents = chunk.GetComponents<IsCanvas>(canvasType);
                    ComponentEnumerator<Position> positionComponents = chunk.GetComponents<Position>(positionType);
                    ComponentEnumerator<Scale> scaleComponents = chunk.GetComponents<Scale>(scaleType);
                    for (int i = 0; i < entities.Length; i++)
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

            for (int i = 0; i < destroyedCanvasCount; i++)
            {
                world.DestroyEntity(destroyedCanvases[i]);
            }
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
        }
    }
}