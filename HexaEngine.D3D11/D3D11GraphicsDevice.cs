﻿namespace HexaEngine.D3D11
{
    using HexaEngine.Core.Debugging;
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.Windows;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using Silk.NET.SDL;
    using System;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using D3D11SubresourceData = Silk.NET.Direct3D11.SubresourceData;
    using Format = Core.Graphics.Format;
    using Query = Core.Graphics.Query;
    using ResourceMiscFlag = Core.Graphics.ResourceMiscFlag;
    using SubresourceData = Core.Graphics.SubresourceData;
    using Usage = Core.Graphics.Usage;

    public unsafe partial class D3D11GraphicsDevice : IGraphicsDevice, IGraphicsDevice1
    {
        internal readonly D3D11 D3D11;

        protected readonly DXGIAdapterD3D11 adapter;
        protected bool disposedValue;

        public static readonly ShaderCompiler Compiler;
        private long graphicsMemoryUsage;
        public ComPtr<ID3D11Device5> Device;
        public ComPtr<ID3D11DeviceContext4> DeviceContext;

        internal ComPtr<ID3D11Debug> Debug;

        static D3D11GraphicsDevice()
        {
            Compiler = new();
        }

        protected D3D11GraphicsDevice(DXGIAdapterD3D11 adapter)
        {
            this.adapter = adapter;
            D3D11 = D3D11.GetApi(adapter.source);
            TextureLoader = new D3D11TextureLoader(this);
        }

        public D3D11GraphicsDevice(DXGIAdapterD3D11 adapter, bool debug)
        {
            this.adapter = adapter;

            D3D11 = D3D11.GetApi(adapter.source);
            D3DFeatureLevel[] levelsArr = new D3DFeatureLevel[]
            {
                D3DFeatureLevel.Level111,
                D3DFeatureLevel.Level110
            };

            CreateDeviceFlag flags = CreateDeviceFlag.BgraSupport;

            if (debug)
            {
                flags |= CreateDeviceFlag.Debug;
            }

            ID3D11Device* tempDevice;
            ID3D11DeviceContext* tempContext;

            D3DFeatureLevel level = 0;
            D3DFeatureLevel* levels = (D3DFeatureLevel*)Unsafe.AsPointer(ref levelsArr[0]);

            D3D11.CreateDevice((IDXGIAdapter*)adapter.IDXGIAdapter.Handle, D3DDriverType.Unknown, IntPtr.Zero, (uint)flags, levels, (uint)levelsArr.Length, D3D11.SdkVersion, &tempDevice, &level, &tempContext).ThrowHResult();
            Level = level;

            tempDevice->QueryInterface(out Device);
            tempContext->QueryInterface(out DeviceContext);

            tempDevice->Release();
            tempContext->Release();

            NativePointer = new(Device.Handle);

#if DEBUG
            if (debug)
            {
                Device.QueryInterface(out Debug);
            }
#endif

            Context = new D3D11GraphicsContext(this);
            TextureLoader = new D3D11TextureLoader(this);
            Profiler = new D3D11GPUProfiler(Device);
        }

        public virtual GraphicsBackend Backend => GraphicsBackend.D3D11;

        public IGraphicsContext Context { get; protected set; }

        public ITextureLoader TextureLoader { get; }

        public IGPUProfiler Profiler { get; }

        public long GraphicsMemoryUsage => graphicsMemoryUsage;

        public string? DebugName { get; set; } = string.Empty;

        public nint NativePointer { get; protected set; }

        public bool IsDisposed => disposedValue;

        public event EventHandler? OnDisposed;

        public D3DFeatureLevel Level { get; protected set; }

        public ICombinedTex2D CreateTex2D(CombinedTex2DDesc desc)
        {
            return new D3D11CombinedTex2D(this, desc);
        }

        public ISwapChain CreateSwapChain(SdlWindow window)
        {
            return adapter.CreateSwapChainForWindow(this, window);
        }

        public ISwapChain CreateSwapChain(Window* window)
        {
            return adapter.CreateSwapChainForWindow(this, window);
        }

        public ISwapChain CreateSwapChain(SdlWindow window, SwapChainDescription swapChainDescription, SwapChainFullscreenDescription fullscreenDescription)
        {
            return adapter.CreateSwapChainForWindow(this, window, swapChainDescription, fullscreenDescription);
        }

        public ISwapChain CreateSwapChain(Window* window, SwapChainDescription swapChainDescription, SwapChainFullscreenDescription fullscreenDescription)
        {
            return adapter.CreateSwapChainForWindow(this, window, swapChainDescription, fullscreenDescription);
        }

        public IComputePipeline CreateComputePipeline(ComputePipelineDesc desc, [CallerFilePath] string filename = "", [CallerLineNumber] int line = 0)
        {
            return new D3D11ComputePipeline(this, desc, $"({nameof(D3D11ComputePipeline)} : {Path.GetFileNameWithoutExtension(filename)}, Line:{line.ToString(CultureInfo.InvariantCulture)})");
        }

        public IGraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDesc desc, [CallerFilePath] string filename = "", [CallerLineNumber] int line = 0)
        {
            return new D3D11GraphicsPipeline(this, desc, $"({nameof(D3D11GraphicsPipeline)} : {Path.GetFileNameWithoutExtension(filename)}, Line:{line.ToString(CultureInfo.InvariantCulture)})");
        }

        public IGraphicsPipelineState CreateGraphicsPipelineState(IGraphicsPipeline pipeline, GraphicsPipelineStateDesc desc, [CallerFilePath] string filename = "", [CallerLineNumber] int line = 0)
        {
            return new D3D11GraphicsPipelineState(this, (D3D11GraphicsPipeline)pipeline, desc, $"({nameof(D3D11GraphicsPipelineState)} : {filename}, Line:{line.ToString(CultureInfo.InvariantCulture)})");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBuffer CreateBuffer(BufferDescription description)
        {
            ComPtr<ID3D11Buffer> buffer;
            BufferDesc desc = Helper.Convert(description);
            Device.CreateBuffer(&desc, (D3D11SubresourceData*)null, &buffer.Handle).ThrowHResult();
            return new D3D11Buffer(buffer, description);
        }

        public IBuffer CreateBuffer(void* src, uint length, BufferDescription description)
        {
            ComPtr<ID3D11Buffer> buffer;
            description.ByteWidth = (int)length;
            BufferDesc desc = Helper.Convert(description);
            var data = Helper.Convert(new SubresourceData(src, description.ByteWidth));
            Device.CreateBuffer(&desc, &data, &buffer.Handle).ThrowHResult();
            return new D3D11Buffer(buffer, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBuffer CreateBuffer<T>(T value, BufferDescription description) where T : unmanaged
        {
            if (description.ByteWidth == 0)
            {
                description.ByteWidth = sizeof(T);
            }

            ComPtr<ID3D11Buffer> buffer;
            BufferDesc desc = Helper.Convert(description);

            D3D11SubresourceData bufferData = new(&value, (uint)description.ByteWidth);

            Device.CreateBuffer(&desc, bufferData, &buffer.Handle).ThrowHResult();

            return new D3D11Buffer(buffer, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBuffer CreateBuffer<T>(T value, BindFlags bindFlags, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag miscFlags = ResourceMiscFlag.None) where T : unmanaged
        {
            BufferDescription description = new(0, bindFlags, usage, cpuAccessFlags, miscFlags);
            return CreateBuffer(value, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBuffer CreateBuffer<T>(T* values, uint count, BufferDescription description) where T : unmanaged
        {
            uint size = (uint)(sizeof(T) * count);
            ComPtr<ID3D11Buffer> buffer;
            description.ByteWidth = (int)size;
            BufferDesc desc = Helper.Convert(description);
            var data = Helper.Convert(new SubresourceData(values, description.ByteWidth));
            Device.CreateBuffer(&desc, &data, &buffer.Handle).ThrowHResult();
            return new D3D11Buffer(buffer, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBuffer CreateBuffer<T>(T* values, uint count, BindFlags bindFlags, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag miscFlags = ResourceMiscFlag.None) where T : unmanaged
        {
            BufferDescription description = new(0, bindFlags, usage, cpuAccessFlags, miscFlags);
            return CreateBuffer(values, count, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDepthStencilView CreateDepthStencilView(IResource resource)
        {
            DepthStencilViewDescription description;
            if (resource is ITexture1D texture1d)
            {
                description = new(texture1d, texture1d.Description.ArraySize > 1);
            }
            else if (resource is ITexture2D texture2d)
            {
                DepthStencilViewDimension dimension;
                if (texture2d.Description.ArraySize > 1)
                {
                    if (texture2d.Description.SampleDescription.Count > 1)
                    {
                        dimension = DepthStencilViewDimension.Texture2DMultisampledArray;
                    }
                    else
                    {
                        dimension = DepthStencilViewDimension.Texture2DArray;
                    }
                }
                else
                {
                    dimension = DepthStencilViewDimension.Texture2D;
                }

                description = new(texture2d, dimension);
            }
            else
            {
                throw new NotSupportedException();
            }

            return CreateDepthStencilView(resource, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDepthStencilView CreateDepthStencilView(IResource resource, DepthStencilViewDescription description)
        {
            ComPtr<ID3D11DepthStencilView> view;
            var desc = Helper.Convert(description);
            Device.CreateDepthStencilView((ID3D11Resource*)resource.NativePointer, &desc, &view.Handle).ThrowHResult();
            return new D3D11DepthStencilView(view, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IRenderTargetView CreateRenderTargetView(IResource resource)
        {
            RenderTargetViewDescription description;
            if (resource is IBuffer)
            {
                throw new NotImplementedException();
            }
            else if (resource is ITexture1D texture1d)
            {
                description = new(texture1d, texture1d.Description.ArraySize > 1);
            }
            else if (resource is ITexture2D texture2d)
            {
                RenderTargetViewDimension dimension;
                if (texture2d.Description.ArraySize > 1)
                {
                    if (texture2d.Description.SampleDescription.Count > 1)
                    {
                        dimension = RenderTargetViewDimension.Texture2DMultisampledArray;
                    }
                    else
                    {
                        dimension = RenderTargetViewDimension.Texture2DArray;
                    }
                }
                else
                {
                    dimension = RenderTargetViewDimension.Texture2D;
                }

                description = new(texture2d, dimension);
            }
            else if (resource is ITexture3D texture3d)
            {
                description = new(texture3d);
            }
            else
            {
                throw new NotSupportedException();
            }

            return CreateRenderTargetView(resource, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IRenderTargetView CreateRenderTargetView(IResource resource, RenderTargetViewDescription description)
        {
            ComPtr<ID3D11RenderTargetView> rtv;
            var desc = Helper.Convert(description);
            Device.CreateRenderTargetView((ID3D11Resource*)resource.NativePointer, &desc, &rtv.Handle).ThrowHResult();
            return new D3D11RenderTargetView(rtv, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ISamplerState CreateSamplerState(SamplerStateDescription description)
        {
            ComPtr<ID3D11SamplerState> sampler;
            var desc = Helper.Convert(description);
            Device.CreateSamplerState(&desc, &sampler.Handle).ThrowHResult();
            return new D3D11SamplerState(sampler, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IShaderResourceView CreateShaderResourceView(IResource resource)
        {
            ShaderResourceViewDescription description;
            if (resource is IBuffer)
            {
                throw new NotImplementedException();
            }
            else if (resource is ITexture1D texture1d)
            {
                description = new(texture1d, texture1d.Description.ArraySize > 1);
            }
            else if (resource is ITexture2D texture2d)
            {
                ShaderResourceViewDimension dimension;
                if (texture2d.Description.ArraySize > 1)
                {
                    if (texture2d.Description.SampleDescription.Count > 1)
                    {
                        dimension = ShaderResourceViewDimension.Texture2DMultisampledArray;
                    }
                    else
                    {
                        dimension = ShaderResourceViewDimension.Texture2DArray;
                    }
                }
                else
                {
                    dimension = ShaderResourceViewDimension.Texture2D;
                }

                if ((texture2d.Description.MiscFlags & ResourceMiscFlag.TextureCube) != 0)
                {
                    dimension = texture2d.Description.ArraySize / 6 > 1 ? ShaderResourceViewDimension.TextureCubeArray : ShaderResourceViewDimension.TextureCube;
                }
                description = new(texture2d, dimension);
            }
            else if (resource is ITexture3D texture3d)
            {
                description = new(texture3d);
            }
            else
            {
                throw new NotSupportedException();
            }

            return CreateShaderResourceView(resource, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IShaderResourceView CreateShaderResourceView(IResource resource, ShaderResourceViewDescription description)
        {
            ComPtr<ID3D11ShaderResourceView> srv;
            var desc = Helper.Convert(description);
            Device.CreateShaderResourceView((ID3D11Resource*)resource.NativePointer, &desc, &srv.Handle).ThrowHResult();
            return new D3D11ShaderResourceView(srv, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IShaderResourceView CreateShaderResourceView(IBuffer buffer)
        {
            ComPtr<ID3D11ShaderResourceView> srv;
            Device.CreateShaderResourceView((ID3D11Resource*)buffer.NativePointer, (ShaderResourceViewDesc*)null, &srv.Handle).ThrowHResult();
            return new D3D11ShaderResourceView(srv, default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IShaderResourceView CreateShaderResourceView(IBuffer buffer, ShaderResourceViewDescription description)
        {
            ComPtr<ID3D11ShaderResourceView> srv;
            var desc = Helper.Convert(description);
            Device.CreateShaderResourceView((ID3D11Resource*)buffer.NativePointer, &desc, &srv.Handle).ThrowHResult();
            return new D3D11ShaderResourceView(srv, default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture1D CreateTexture1D(Texture1DDescription description)
        {
            ComPtr<ID3D11Texture1D> texture;
            Texture1DDesc desc = Helper.Convert(description);
            Device.CreateTexture1D(&desc, (D3D11SubresourceData*)null, &texture.Handle).ThrowHResult();
            return new D3D11Texture1D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture1D CreateTexture1D(Texture1DDescription description, SubresourceData[]? subresources)
        {
            ComPtr<ID3D11Texture1D> texture;
            Texture1DDesc desc = Helper.Convert(description);
            if (subresources != null)
            {
                D3D11SubresourceData* data = AllocT<D3D11SubresourceData>(subresources.Length);
                Helper.Convert(subresources, data);
                Device.CreateTexture1D(&desc, data, &texture.Handle).ThrowHResult();
                Free(data);
            }
            else
            {
                Device.CreateTexture1D(&desc, (D3D11SubresourceData*)null, &texture.Handle).ThrowHResult();
            }
            return new D3D11Texture1D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture1D CreateTexture1D(Format format, int width, int arraySize, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags, ResourceMiscFlag misc)
        {
            return CreateTexture1D(format, width, arraySize, mipLevels, subresources, bindFlags, Usage.Default, CpuAccessFlags.None, misc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture1D CreateTexture1D(Format format, int width, int arraySize, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags = BindFlags.ShaderResource, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag misc = ResourceMiscFlag.None)
        {
            Texture1DDescription description = new(format, width, arraySize, mipLevels, bindFlags, usage, cpuAccessFlags, misc);
            ComPtr<ID3D11Texture1D> texture;
            Texture1DDesc desc = Helper.Convert(description);

            if (subresources != null)
            {
                D3D11SubresourceData* data = AllocT<D3D11SubresourceData>(subresources.Length);
                Helper.Convert(subresources, data);
                Device.CreateTexture1D(&desc, data, &texture.Handle).ThrowHResult();
                Free(data);
            }
            else
            {
                Device.CreateTexture1D(&desc, (D3D11SubresourceData*)null, &texture.Handle).ThrowHResult();
            }

            return new D3D11Texture1D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture2D CreateTexture2D(Texture2DDescription description)
        {
            ComPtr<ID3D11Texture2D> texture;
            Texture2DDesc desc = Helper.Convert(description);
            Device.CreateTexture2D(&desc, (D3D11SubresourceData*)null, &texture.Handle).ThrowHResult();
            return new D3D11Texture2D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture2D CreateTexture2D(Texture2DDescription description, SubresourceData[]? subresources)
        {
            ComPtr<ID3D11Texture2D> texture;
            Texture2DDesc desc = Helper.Convert(description);
            if (subresources != null)
            {
                D3D11SubresourceData* data = AllocT<D3D11SubresourceData>(subresources.Length);
                Helper.Convert(subresources, data);
                Device.CreateTexture2D(&desc, data, &texture.Handle).ThrowHResult();
                Free(data);
            }
            else
            {
                Device.CreateTexture2D(&desc, (D3D11SubresourceData*)null, &texture.Handle).ThrowHResult();
            }
            return new D3D11Texture2D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture2D CreateTexture2D(Format format, int width, int height, int arraySize, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags, ResourceMiscFlag misc)
        {
            return CreateTexture2D(format, width, height, arraySize, mipLevels, subresources, bindFlags, Usage.Default, CpuAccessFlags.None, 1, 0, misc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture2D CreateTexture2D(Format format, int width, int height, int arraySize, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags = BindFlags.ShaderResource, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, int sampleCount = 1, int sampleQuality = 0, ResourceMiscFlag misc = ResourceMiscFlag.None)
        {
            Texture2DDescription description = new(format, width, height, arraySize, mipLevels, bindFlags, usage, cpuAccessFlags, sampleCount, sampleQuality, misc);
            ComPtr<ID3D11Texture2D> texture;
            Texture2DDesc desc = Helper.Convert(description);

            if (subresources != null)
            {
                D3D11SubresourceData* data = AllocT<D3D11SubresourceData>(subresources.Length);
                Helper.Convert(subresources, data);
                Device.CreateTexture2D(&desc, data, &texture.Handle).ThrowHResult();
                Free(data);
            }
            else
            {
                Device.CreateTexture2D(&desc, (D3D11SubresourceData*)null, &texture.Handle).ThrowHResult();
            }

            return new D3D11Texture2D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture3D CreateTexture3D(Texture3DDescription description)
        {
            ComPtr<ID3D11Texture3D> texture;
            Texture3DDesc desc = Helper.Convert(description);
            Device.CreateTexture3D(&desc, (D3D11SubresourceData*)null, &texture.Handle).ThrowHResult();
            return new D3D11Texture3D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture3D CreateTexture3D(Texture3DDescription description, SubresourceData[]? subresources)
        {
            ComPtr<ID3D11Texture3D> texture;
            Texture3DDesc desc = Helper.Convert(description);
            if (subresources != null)
            {
                D3D11SubresourceData* data = AllocT<D3D11SubresourceData>(subresources.Length);
                Helper.Convert(subresources, data);
                Device.CreateTexture3D(&desc, data, &texture.Handle).ThrowHResult();
            }
            else
            {
                Device.CreateTexture3D(&desc, (D3D11SubresourceData*)null, &texture.Handle).ThrowHResult();
            }
            return new D3D11Texture3D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture3D CreateTexture3D(Format format, int width, int height, int depth, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags, ResourceMiscFlag misc)
        {
            return CreateTexture3D(format, width, height, depth, mipLevels, subresources, bindFlags, Usage.Default, CpuAccessFlags.None, misc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture3D CreateTexture3D(Format format, int width, int height, int depth, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags = BindFlags.ShaderResource, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag misc = ResourceMiscFlag.None)
        {
            Texture3DDescription description = new(format, width, height, depth, mipLevels, bindFlags, usage, cpuAccessFlags, misc);
            ComPtr<ID3D11Texture3D> texture;
            Texture3DDesc desc = Helper.Convert(description);

            if (subresources != null)
            {
                D3D11SubresourceData* data = AllocT<D3D11SubresourceData>(subresources.Length);
                Helper.Convert(subresources, data);
                Device.CreateTexture3D(&desc, data, &texture.Handle).ThrowHResult();
            }
            else
            {
                Device.CreateTexture3D(&desc, (D3D11SubresourceData*)null, &texture.Handle).ThrowHResult();
            }

            return new D3D11Texture3D(texture, description);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                OnDisposed?.Invoke(this, EventArgs.Empty);

                Profiler.Dispose();

                LeakTracer.ReportLiveInstances();

                Context.Dispose();
                Device.Release();

                adapter.Dispose();

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

                if (Debug.Handle != null)
                {
                    Debug.ReportLiveDeviceObjects(RldoFlags.Detail | RldoFlags.IgnoreInternal);
                    Debug.Release();
                }

                D3D11.Dispose();

                disposedValue = true;
            }
        }

        ~D3D11GraphicsDevice()
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IQuery CreateQuery()
        {
            return CreateQuery(Query.Event);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IQuery CreateQuery(Query type)
        {
            ComPtr<ID3D11Query> query;
            QueryDesc desc = new(Helper.Convert(type), 0);
            Device.CreateQuery(&desc, &query.Handle);
            return new D3D11Query(query);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IFence CreateFence(ulong initialValue, FenceFlags flags)
        {
            Device.CreateFence(initialValue, Helper.Convert(flags), out ComPtr<ID3D11Fence> fence);
            return new D3D11Fence(fence);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IGraphicsContext CreateDeferredContext()
        {
            ComPtr<ID3D11DeviceContext3> context;
            Device.CreateDeferredContext3(0, &context.Handle);
            return new D3D11GraphicsContext(this, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IUnorderedAccessView CreateUnorderedAccessView(IResource resource, UnorderedAccessViewDescription description)
        {
            ComPtr<ID3D11UnorderedAccessView> view;
            var desc = Helper.Convert(description);
            Device.CreateUnorderedAccessView((ID3D11Resource*)resource.NativePointer, &desc, &view.Handle);
            return new D3D11UnorderedAccessView(view, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IResourceBindingList CreateRootDescriptorTable(IGraphicsPipeline pipeline)
        {
            return new D3D11ResourceBindingList((D3D11GraphicsPipeline)pipeline);
        }
    }
}