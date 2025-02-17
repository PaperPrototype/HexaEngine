﻿namespace HexaEngine.Vulkan
{
    using HexaEngine.Core.Debugging;
    using HexaEngine.Core.Graphics;
    using System;

    public abstract unsafe class DeviceChildBase : DisposableBase, IDeviceChild
    {
        protected IntPtr nativePointer;

        public virtual string? DebugName { get; set; }

        public IntPtr NativePointer => nativePointer;
    }

    public abstract class DisposableBase : IDisposable
    {
        private bool disposedValue;

        public DisposableBase()
        {
            LeakTracer.Allocate(this);
        }

        public bool IsDisposed => disposedValue;

        public event EventHandler? OnDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                DisposeCore();
                OnDisposed?.Invoke(this, EventArgs.Empty);
                LeakTracer.Release(this);
                disposedValue = true;
            }
        }

        protected abstract void DisposeCore();

        ~DisposableBase()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}