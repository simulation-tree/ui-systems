using Collections.Generic;
using Simulation;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UI.Components;
using UI.Messages;
using Worlds;

namespace UI.Systems
{
    //todo: could this system be part of the automation repo instead?
    public partial class ComponentMixingSystem : SystemBase, IListener<UIUpdate>
    {
        private static readonly MixFunction[] functions;

        unsafe static ComponentMixingSystem()
        {
            functions = new MixFunction[13];
            functions[(byte)ComponentMix.Operation.UnsignedAdd] = new(&UnsignedAdd);
            functions[(byte)ComponentMix.Operation.UnsignedSubtract] = new(&UnsignedSubtract);
            functions[(byte)ComponentMix.Operation.UnsignedMultiply] = new(&UnsignedMultiply);
            functions[(byte)ComponentMix.Operation.UnsignedDivide] = new(&UnsignedDivide);
            functions[(byte)ComponentMix.Operation.SignedAdd] = new(&SignedAdd);
            functions[(byte)ComponentMix.Operation.SignedSubtract] = new(&SignedSubtract);
            functions[(byte)ComponentMix.Operation.SignedMultiply] = new(&SignedMultiply);
            functions[(byte)ComponentMix.Operation.SignedDivide] = new(&SignedDivide);
            functions[(byte)ComponentMix.Operation.FloatingAdd] = new(&FloatingAdd);
            functions[(byte)ComponentMix.Operation.FloatingSubtract] = new(&FloatingSubtract);
            functions[(byte)ComponentMix.Operation.FloatingMultiply] = new(&FloatingMultiply);
            functions[(byte)ComponentMix.Operation.FloatingDivide] = new(&FloatingDivide);
        }

        private readonly World world;
        private readonly List<Request> requests;

        public ComponentMixingSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            requests = new(4);
        }

        public override void Dispose()
        {
            requests.Dispose();
        }

        void IListener<UIUpdate>.Receive(ref UIUpdate message)
        {
            ComponentQuery<ComponentMix> query = new(world);
            query.ExcludeDisabled(true);
            foreach (var x in query)
            {
                uint entity = x.entity;
                ref ComponentMix mix = ref x.component1;
                requests.Add(new(entity, mix));
            }

            MixComponents(requests.AsSpan());
            requests.Clear();
        }

        private void MixComponents(ReadOnlySpan<Request> requests)
        {
            Span<MixFunction> functionsSpan = functions.AsSpan();
            foreach (Request request in requests)
            {
                uint entity = request.entity;
                ComponentMix mix = request.mix;
                DataType leftType = mix.left;
                DataType rightType = mix.right;
                DataType outputType = mix.output;
                ThrowIfComponentIsMissing(world, entity, leftType);
                ThrowIfComponentIsMissing(world, entity, rightType);
                ThrowIfComponentSizesDontMatch(leftType, rightType);
                ThrowIfComponentSizesDontMatch(leftType, outputType);
                int componentType = outputType.index;
                if (!world.ContainsComponent(entity, componentType))
                {
                    world.AddComponentType(entity, componentType);
                }

                Span<byte> leftBytes = world.GetComponentBytes(entity, leftType.index);
                Span<byte> rightBytes = world.GetComponentBytes(entity, rightType.index);
                Span<byte> outputBytes = world.GetComponentBytes(entity, componentType);
                ushort componentSize = leftType.size;
                byte partCount = mix.vectorLength;
                int partSize = componentSize / partCount;
                for (int i = 0; i < partCount; i++)
                {
                    Span<byte> leftPart = leftBytes.Slice(i * partSize, partSize);
                    Span<byte> rightPart = rightBytes.Slice(i * partSize, partSize);
                    Span<byte> outputPart = outputBytes.Slice(i * partSize, partSize);
                    functionsSpan[(byte)mix.operation].Invoke(leftPart, rightPart, outputPart);
                }
            }
        }

        //todo: efficiency: the branches for different sizes can be optimized away by having more functions
        [UnmanagedCallersOnly]
        private static void UnsignedAdd(MixFunction.Input input)
        {
            byte length = input.size;
            if (length == 1)
            {
                ref byte leftPart = ref input.GetLeft<byte>();
                ref byte rightPart = ref input.GetRight<byte>();
                ref byte outputPart = ref input.GetOutput<byte>();
                outputPart = (byte)(leftPart + rightPart);
            }
            else if (length == 2)
            {
                ref ushort leftPart = ref input.GetLeft<ushort>();
                ref ushort rightPart = ref input.GetRight<ushort>();
                ref ushort outputPart = ref input.GetOutput<ushort>();
                outputPart = (ushort)(leftPart + rightPart);
            }
            else if (length == 4)
            {
                ref uint leftPart = ref input.GetLeft<uint>();
                ref uint rightPart = ref input.GetRight<uint>();
                ref uint outputPart = ref input.GetOutput<uint>();
                outputPart = leftPart + rightPart;
            }
            else
            {
                throw new Exception($"Part size `{length}` is not supported");
            }
        }

        [UnmanagedCallersOnly]
        private static void SignedAdd(MixFunction.Input input)
        {
            byte length = input.size;
            if (length == 1)
            {
                ref sbyte leftPart = ref input.GetLeft<sbyte>();
                ref sbyte rightPart = ref input.GetRight<sbyte>();
                ref sbyte outputPart = ref input.GetOutput<sbyte>();
                outputPart = (sbyte)(leftPart + rightPart);
            }
            else if (length == 2)
            {
                ref short leftPart = ref input.GetLeft<short>();
                ref short rightPart = ref input.GetRight<short>();
                ref short outputPart = ref input.GetOutput<short>();
                outputPart = (short)(leftPart + rightPart);
            }
            else if (length == 4)
            {
                ref int leftPart = ref input.GetLeft<int>();
                ref int rightPart = ref input.GetRight<int>();
                ref int outputPart = ref input.GetOutput<int>();
                outputPart = leftPart + rightPart;
            }
            else
            {
                throw new Exception($"Part size `{length}` is not supported");
            }
        }

        [UnmanagedCallersOnly]
        private static void UnsignedSubtract(MixFunction.Input input)
        {
            byte length = input.size;
            if (length == 1)
            {
                ref byte leftPart = ref input.GetLeft<byte>();
                ref byte rightPart = ref input.GetRight<byte>();
                ref byte outputPart = ref input.GetOutput<byte>();
                outputPart = (byte)(leftPart - rightPart);
            }
            else if (length == 2)
            {
                ref ushort leftPart = ref input.GetLeft<ushort>();
                ref ushort rightPart = ref input.GetRight<ushort>();
                ref ushort outputPart = ref input.GetOutput<ushort>();
                outputPart = (ushort)(leftPart - rightPart);
            }
            else if (length == 4)
            {
                ref uint leftPart = ref input.GetLeft<uint>();
                ref uint rightPart = ref input.GetRight<uint>();
                ref uint outputPart = ref input.GetOutput<uint>();
                outputPart = leftPart - rightPart;
            }
            else
            {
                throw new Exception($"Part size `{length}` is not supported");
            }
        }

        [UnmanagedCallersOnly]
        private static void SignedSubtract(MixFunction.Input input)
        {
            byte length = input.size;
            if (length == 1)
            {
                ref sbyte leftPart = ref input.GetLeft<sbyte>();
                ref sbyte rightPart = ref input.GetRight<sbyte>();
                ref sbyte outputPart = ref input.GetOutput<sbyte>();
                outputPart = (sbyte)(leftPart - rightPart);
            }
            else if (length == 2)
            {
                ref short leftPart = ref input.GetLeft<short>();
                ref short rightPart = ref input.GetRight<short>();
                ref short outputPart = ref input.GetOutput<short>();
                outputPart = (short)(leftPart - rightPart);
            }
            else if (length == 4)
            {
                ref int leftPart = ref input.GetLeft<int>();
                ref int rightPart = ref input.GetRight<int>();
                ref int outputPart = ref input.GetOutput<int>();
                outputPart = leftPart - rightPart;
            }
            else
            {
                throw new Exception($"Part size `{length}` is not supported");
            }
        }

        [UnmanagedCallersOnly]
        private static void UnsignedMultiply(MixFunction.Input input)
        {
            byte length = input.size;
            if (length == 1)
            {
                ref byte leftPart = ref input.GetLeft<byte>();
                ref byte rightPart = ref input.GetRight<byte>();
                ref byte outputPart = ref input.GetOutput<byte>();
                outputPart = (byte)(leftPart * rightPart);
            }
            else if (length == 2)
            {
                ref ushort leftPart = ref input.GetLeft<ushort>();
                ref ushort rightPart = ref input.GetRight<ushort>();
                ref ushort outputPart = ref input.GetOutput<ushort>();
                outputPart = (ushort)(leftPart * rightPart);
            }
            else if (length == 4)
            {
                ref uint leftPart = ref input.GetLeft<uint>();
                ref uint rightPart = ref input.GetRight<uint>();
                ref uint outputPart = ref input.GetOutput<uint>();
                outputPart = leftPart * rightPart;
            }
            else
            {
                throw new Exception($"Part size `{length}` is not supported");
            }
        }

        [UnmanagedCallersOnly]
        private static void SignedMultiply(MixFunction.Input input)
        {
            byte length = input.size;
            if (length == 1)
            {
                ref sbyte leftPart = ref input.GetLeft<sbyte>();
                ref sbyte rightPart = ref input.GetRight<sbyte>();
                ref sbyte outputPart = ref input.GetOutput<sbyte>();
                outputPart = (sbyte)(leftPart * rightPart);
            }
            else if (length == 2)
            {
                ref short leftPart = ref input.GetLeft<short>();
                ref short rightPart = ref input.GetRight<short>();
                ref short outputPart = ref input.GetOutput<short>();
                outputPart = (short)(leftPart * rightPart);
            }
            else if (length == 4)
            {
                ref int leftPart = ref input.GetLeft<int>();
                ref int rightPart = ref input.GetRight<int>();
                ref int outputPart = ref input.GetOutput<int>();
                outputPart = leftPart * rightPart;
            }
            else
            {
                throw new Exception($"Part size `{length}` is not supported");
            }
        }

        [UnmanagedCallersOnly]
        private static void UnsignedDivide(MixFunction.Input input)
        {
            byte length = input.size;
            if (length == 1)
            {
                ref byte leftPart = ref input.GetLeft<byte>();
                ref byte rightPart = ref input.GetRight<byte>();
                ref byte outputPart = ref input.GetOutput<byte>();
                outputPart = (byte)(leftPart / rightPart);
            }
            else if (length == 2)
            {
                ref ushort leftPart = ref input.GetLeft<ushort>();
                ref ushort rightPart = ref input.GetRight<ushort>();
                ref ushort outputPart = ref input.GetOutput<ushort>();
                outputPart = (ushort)(leftPart / rightPart);
            }
            else if (length == 4)
            {
                ref uint leftPart = ref input.GetLeft<uint>();
                ref uint rightPart = ref input.GetRight<uint>();
                ref uint outputPart = ref input.GetOutput<uint>();
                outputPart = leftPart / rightPart;
            }
            else
            {
                throw new Exception($"Part size `{length}` is not supported");
            }
        }

        [UnmanagedCallersOnly]
        private static void SignedDivide(MixFunction.Input input)
        {
            byte length = input.size;
            if (length == 1)
            {
                ref sbyte leftPart = ref input.GetLeft<sbyte>();
                ref sbyte rightPart = ref input.GetRight<sbyte>();
                ref sbyte outputPart = ref input.GetOutput<sbyte>();
                outputPart = (sbyte)(leftPart / rightPart);
            }
            else if (length == 2)
            {
                ref short leftPart = ref input.GetLeft<short>();
                ref short rightPart = ref input.GetRight<short>();
                ref short outputPart = ref input.GetOutput<short>();
                outputPart = (short)(leftPart / rightPart);
            }
            else if (length == 4)
            {
                ref int leftPart = ref input.GetLeft<int>();
                ref int rightPart = ref input.GetRight<int>();
                ref int outputPart = ref input.GetOutput<int>();
                outputPart = leftPart / rightPart;
            }
            else
            {
                throw new Exception($"Part size `{length}` is not supported");
            }
        }

        [UnmanagedCallersOnly]
        private static void FloatingAdd(MixFunction.Input input)
        {
            byte length = input.size;
            if (length == 4)
            {
                ref float leftPart = ref input.GetLeft<float>();
                ref float rightPart = ref input.GetRight<float>();
                ref float outputPart = ref input.GetOutput<float>();
                outputPart = leftPart + rightPart;
            }
            else
            {
                throw new Exception($"Part size `{length}` is not supported");
            }
        }

        [UnmanagedCallersOnly]
        private static void FloatingSubtract(MixFunction.Input input)
        {
            byte length = input.size;
            if (length == 4)
            {
                ref float leftPart = ref input.GetLeft<float>();
                ref float rightPart = ref input.GetRight<float>();
                ref float outputPart = ref input.GetOutput<float>();
                outputPart = leftPart - rightPart;
            }
            else
            {
                throw new Exception($"Part size `{length}` is not supported");
            }
        }

        [UnmanagedCallersOnly]
        private static void FloatingMultiply(MixFunction.Input input)
        {
            byte length = input.size;
            if (length == 4)
            {
                ref float leftPart = ref input.GetLeft<float>();
                ref float rightPart = ref input.GetRight<float>();
                ref float outputPart = ref input.GetOutput<float>();
                outputPart = leftPart * rightPart;
            }
            else
            {
                throw new Exception($"Part size `{length}` is not supported");
            }
        }

        [UnmanagedCallersOnly]
        private static void FloatingDivide(MixFunction.Input input)
        {
            byte length = input.size;
            if (length == 4)
            {
                ref float leftPart = ref input.GetLeft<float>();
                ref float rightPart = ref input.GetRight<float>();
                ref float outputPart = ref input.GetOutput<float>();
                outputPart = leftPart / rightPart;
            }
            else
            {
                throw new Exception($"Part size `{length}` is not supported");
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfComponentIsMissing(World world, uint entity, DataType componentType)
        {
            if (!world.ContainsComponent(entity, componentType.index))
            {
                throw new Exception($"Entity `{entity}` is missing expected component `{componentType.ToString(world.Schema)}`");
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfComponentSizesDontMatch(DataType left, DataType right)
        {
            if (left.size != right.size)
            {
                throw new Exception($"Components `{left}` and `{right}` don't match in size");
            }
        }

        public readonly struct Request
        {
            public readonly uint entity;
            public readonly ComponentMix mix;

            public Request(uint entity, ComponentMix mix)
            {
                this.entity = entity;
                this.mix = mix;
            }
        }
    }

    public readonly unsafe struct MixFunction
    {
        private readonly delegate* unmanaged<Input, void> function;

        public MixFunction(delegate* unmanaged<Input, void> function)
        {
            this.function = function;
        }

        public readonly void Invoke(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, ReadOnlySpan<byte> output)
        {
            function(new(left, right, output));
        }

        public readonly struct Input
        {
            public readonly byte size;

            private readonly byte* left;
            private readonly byte* right;
            private readonly byte* output;

            public Input(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, ReadOnlySpan<byte> output)
            {
                size = (byte)output.Length;
                this.left = left.GetPointer();
                this.right = right.GetPointer();
                this.output = output.GetPointer();
            }

            public readonly ref T GetLeft<T>() where T : unmanaged
            {
                return ref *(T*)left;
            }

            public readonly ref T GetRight<T>() where T : unmanaged
            {
                return ref *(T*)right;
            }

            public readonly ref T GetOutput<T>() where T : unmanaged
            {
                return ref *(T*)output;
            }
        }
    }
}