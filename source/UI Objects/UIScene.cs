using Collections.Generic;
using System;
using Types;
using Unmanaged;

namespace UI.Systems
{
    internal readonly struct UIScene : IDisposable
    {
        private readonly List<UIValue> values;

        public readonly ReadOnlySpan<UIValue.Borrowed> Values => values.AsSpan().As<UIValue, UIValue.Borrowed>();

        public UIScene()
        {
            values = new();
        }

        public readonly void Dispose()
        {
            foreach (UIValue value in values)
            {
                value.Dispose();
            }

            values.Dispose();
        }

        public readonly void Add<T>(ReadOnlySpan<char> name, T value) where T : unmanaged
        {
            values.Add(UIValue.Create(name, value));
        }

        public readonly void Add(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            values.Add(new(name, value));
        }

        public readonly bool TryGet(ReadOnlySpan<char> name, out UIValue.Borrowed value)
        {
            Span<UIValue> nodes = this.values.AsSpan();
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].Name.SequenceEqual(name))
                {
                    value = new(nodes[i]);
                    return true;
                }
            }

            value = default;
            return false;
        }
    }

    internal readonly struct UIValue : IDisposable
    {
        public readonly TypeMetadata type;

        private readonly int nameLength;
        private readonly int valueLength;
        private readonly MemoryAddress allocation;

        public readonly ReadOnlySpan<char> Name => allocation.GetSpan<char>(nameLength);

        private UIValue(int nameLength, int valueLength, MemoryAddress allocation, TypeMetadata type)
        {
            this.nameLength = nameLength;
            this.valueLength = valueLength;
            this.allocation = allocation;
            this.type = type;
        }

        public UIValue(ReadOnlySpan<char> name, ReadOnlySpan<char> text)
        {
            nameLength = name.Length;
            valueLength = text.Length;
            allocation = MemoryAddress.Allocate((nameLength + valueLength) * sizeof(char));
            allocation.CopyFrom(name);
            allocation.CopyFrom(text, nameLength * sizeof(char));
            type = default;
        }

        public readonly void Dispose()
        {
            if (type.Is<UIValue>())
            {
                UIValue child = GetValue<UIValue>();
                child.Dispose();
            }

            allocation.Dispose();
        }

        public readonly ReadOnlySpan<char> GetText()
        {
            return allocation.GetSpan<char>(nameLength);
        }

        public readonly ref T GetValue<T>() where T : unmanaged
        {
            return ref allocation.Read<T>(nameLength * sizeof(char));
        }

        public unsafe static UIValue Create<T>(ReadOnlySpan<char> name, T value) where T : unmanaged
        {
            int nameLength = name.Length;
            int valueLength = sizeof(T);
            MemoryAddress allocation = MemoryAddress.Allocate((nameLength * sizeof(char)) + valueLength);
            allocation.CopyFrom(name);
            allocation.Write(nameLength * sizeof(char), value);
            TypeMetadata type = TypeMetadata.GetOrRegister<T>();
            return new UIValue(nameLength, -1, allocation, type);
        }

        public readonly struct Borrowed
        {
            private readonly UIValue node;

            public readonly TypeMetadata Type => node.type;

            public Borrowed(UIValue node)
            {
                this.node = node;
            }

            public readonly ReadOnlySpan<char> GetText()
            {
                return node.GetText();
            }

            public readonly ref T GetValue<T>() where T : unmanaged
            {
                return ref node.GetValue<T>();
            }
        }
    }
}