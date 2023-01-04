﻿namespace HexaEngine.Graphics.Buffers
{
    using HexaEngine.Core.Graphics;
    using System;
    using System.Runtime.CompilerServices;

    public unsafe class VertexBuffer<T> : IBuffer where T : unmanaged
    {
        private const int DefaultCapacity = 8;

        private readonly IGraphicsDevice device;
        private readonly string name;

        private IBuffer buffer;
        private BufferDescription description;

        private T* items;
        private uint count;
        private uint capacity;

        private bool isDirty;

        private bool disposedValue;

        public VertexBuffer(IGraphicsDevice device, CpuAccessFlags flags, [CallerFilePath] string name = "")
        {
            this.device = device;
            this.name = name;

            items = Alloc<T>(DefaultCapacity);
            ZeroRange(items, DefaultCapacity);
            capacity = DefaultCapacity;

            description = new(sizeof(T) * DefaultCapacity, BindFlags.VertexBuffer, Usage.Default, flags);
            if (flags.HasFlag(CpuAccessFlags.Write))
            {
                description.Usage = Usage.Dynamic;
            }
            if (flags.HasFlag(CpuAccessFlags.Read))
            {
                description.Usage = Usage.Staging;
            }
            if (flags.HasFlag(CpuAccessFlags.None))
            {
                throw new InvalidOperationException("If cpu access flags are none initial data must be provided");
            }

            buffer = device.CreateBuffer(description);
        }

        public VertexBuffer(IGraphicsDevice device, CpuAccessFlags flags, T[] vertices, [CallerFilePath] string name = "")
        {
            this.device = device;
            this.name = name;

            capacity = (uint)vertices.Length;
            items = Alloc<T>(capacity);
            count = capacity;

            for (uint i = 0; i < capacity; i++)
            {
                items[i] = vertices[i];
            }

            description = new(sizeof(T) * (int)capacity, BindFlags.VertexBuffer, Usage.Default, flags);
            if (flags.HasFlag(CpuAccessFlags.Write))
            {
                description.Usage = Usage.Dynamic;
            }
            if (flags.HasFlag(CpuAccessFlags.Read))
            {
                description.Usage = Usage.Staging;
            }
            if (flags.HasFlag(CpuAccessFlags.None))
            {
                description.Usage = Usage.Immutable;
            }

            buffer = device.CreateBuffer(items, capacity, description);
        }

        public VertexBuffer(IGraphicsDevice device, CpuAccessFlags flags, uint capacity, [CallerFilePath] string name = "")
        {
            this.device = device;
            this.name = name;

            this.capacity = capacity;
            items = Alloc<T>(capacity);
            ZeroRange(items, capacity);

            description = new(sizeof(T) * (int)capacity, BindFlags.VertexBuffer, Usage.Default, flags);
            if (flags.HasFlag(CpuAccessFlags.Write))
            {
                description.Usage = Usage.Dynamic;
            }
            if (flags.HasFlag(CpuAccessFlags.Read))
            {
                description.Usage = Usage.Staging;
            }
            if (flags.HasFlag(CpuAccessFlags.None))
            {
                description.Usage = Usage.Immutable;
            }

            buffer = device.CreateBuffer(items, capacity, description);
        }

        public event EventHandler? OnDisposed
        {
            add
            {
                buffer.OnDisposed += value;
            }

            remove
            {
                buffer.OnDisposed -= value;
            }
        }

        public uint Count => count;

        public uint Stride { get; } = (uint)sizeof(T);

        public uint Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => capacity;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var tmp = Alloc<T>((int)value);
                var oldsize = count * sizeof(T);
                var newsize = value * sizeof(T);
                System.Buffer.MemoryCopy(items, tmp, newsize, oldsize > newsize ? newsize : oldsize);
                Free(items);
                items = tmp;
                capacity = value;
                count = capacity < count ? capacity : count;
                buffer.Dispose();
                buffer = device.CreateBuffer(items, capacity, description);
            }
        }

        public BufferDescription Description => buffer.Description;

        public int Length => buffer.Length;

        public ResourceDimension Dimension => buffer.Dimension;

        public nint NativePointer => buffer.NativePointer;

        public string? DebugName { get => buffer.DebugName; set => buffer.DebugName = value; }

        public bool IsDisposed => buffer.IsDisposed;

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => items[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                items[index] = value;
                isDirty = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetCounter()
        {
            count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(uint capacity)
        {
            if (this.capacity < capacity)
            {
                Grow(capacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow(uint capacity)
        {
            uint newcapacity = count == 0 ? DefaultCapacity : 2 * count;

            if (newcapacity < capacity) newcapacity = capacity;

            Capacity = newcapacity;
        }

        public void Add(params T[] vertices)
        {
            uint index = count;
            count += (uint)vertices.Length;
            EnsureCapacity(count);

            for (int i = 0; i < vertices.Length; i++)
            {
                items[index + i] = vertices[i];
            }

            isDirty = true;
        }

        public void Remove(int index)
        {
            var size = (count - index) * sizeof(T);
            System.Buffer.MemoryCopy(&items[index + 1], &items[index], size, size);
            isDirty = true;
        }

        public bool Update(IGraphicsContext context)
        {
            if (isDirty)
            {
                context.Write(buffer, items, (int)(count * sizeof(T)));
                isDirty = false;
                return true;
            }
            return false;
        }

        public void Clear()
        {
            count = 0;
            isDirty = true;
        }

        public void FlushMemory()
        {
            Free(items);
        }

        public void CopyTo(IGraphicsContext context, IBuffer buffer)
        {
            context.CopyResource(buffer, this.buffer);
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                buffer?.Dispose();
                capacity = 0;
                count = 0;
                Free(items);

                disposedValue = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}