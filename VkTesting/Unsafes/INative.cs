﻿namespace VkTesting.Unsafes
{
    using System;
    using System.Runtime.CompilerServices;

    public interface INative : IDisposable
    {
        public nint NativePointer { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }
    }
}