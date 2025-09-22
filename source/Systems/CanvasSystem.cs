using Cameras;
using Rendering;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Transforms.Components;
using UI.Components;
using UI.Messages;
using Worlds;

namespace UI.Systems
{
    [SkipLocalsInit]
    public partial class CanvasSystem : SystemBase, IListener<UIUpdate>
    {
        private readonly World world;
        private readonly int canvasType;
        private readonly int positionType;
        private readonly int scaleType;
        private readonly BitMask canvasComponents;

        public CanvasSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            Schema schema = world.Schema;
            canvasType = schema.GetComponentType<IsCanvas>();
            positionType = schema.GetComponentType<Position>();
            scaleType = schema.GetComponentType<Scale>();
            canvasComponents = new(canvasType, positionType, scaleType);
        }

        public override void Dispose()
        {
        }

        void IListener<UIUpdate>.Receive(ref UIUpdate message)
        {
            Span<uint> destroyedCanvases = stackalloc uint[world.CountChunks(canvasComponents)];
            int destroyedCanvasCount = 0;
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.ComponentTypes.ContainsAll(canvasComponents) && !chunk.IsDisabled)
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
                            Camera camera = Entity.Get<Camera>(world, cameraEntity);
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
    }
}