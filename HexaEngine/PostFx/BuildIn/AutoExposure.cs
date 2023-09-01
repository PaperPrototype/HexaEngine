﻿#nullable disable

namespace HexaEngine.Effects.BuildIn
{
    using HexaEngine.Core;
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.Graphics.Buffers;
    using HexaEngine.Graph;
    using HexaEngine.Mathematics;
    using HexaEngine.PostFx;
    using HexaEngine.Rendering.Graph;
    using System;
    using System.Numerics;

    public class AutoExposure : PostFxBase
    {
        private bool initialized = false;
        private GraphResourceBuilder creator;
        private int width;
        private int height;
        private bool dirty;
        private IComputePipeline lumaCompute;
        private ConstantBuffer<LumaParams> lumaParams;
        private UavBuffer<uint> histogram;

        private IComputePipeline lumaAvgCompute;
        private ConstantBuffer<LumaAvgParams> lumaAvgParams;
        private ResourceRef<Texture2D> lumaTex;

        public IShaderResourceView Input;

        private float minLogLuminance = -8;
        private float maxLogLuminance = 3;
        private float tau = 1.1f;

        public override string Name => "AutoExposure";

        public override PostFxFlags Flags => PostFxFlags.NoOutput;

        public unsafe float MinLogLuminance
        {
            get => minLogLuminance;
            set => NotifyPropertyChangedAndSet(ref minLogLuminance, value);
        }

        public unsafe float MaxLogLuminance
        {
            get => maxLogLuminance;
            set => NotifyPropertyChangedAndSet(ref maxLogLuminance, value);
        }

        public unsafe float Tau
        {
            get => tau;
            set => NotifyPropertyChangedAndSet(ref tau, value);
        }

        #region Structs

        private struct LumaParams
        {
            public uint InputWidth;
            public uint InputHeight;
            public float MinLogLuminance;
            public float OneOverLogLuminanceRange;

            public LumaParams(uint width, uint height)
            {
                InputWidth = width;
                InputHeight = height;
                MinLogLuminance = -8.0f;
                OneOverLogLuminanceRange = 1.0f / (3.0f + 8.0f);
            }

            public LumaParams(uint inputWidth, uint inputHeight, float minLogLuminance, float oneOverLogLuminanceRange) : this(inputWidth, inputHeight)
            {
                MinLogLuminance = minLogLuminance;
                OneOverLogLuminanceRange = oneOverLogLuminanceRange;
            }
        }

        private struct LumaAvgParams
        {
            public uint PixelCount;
            public float MinLogLuminance;
            public float LogLuminanceRange;
            public float TimeDelta;
            public float Tau;
            public Vector3 Padd;

            public LumaAvgParams(uint pixelCount)
            {
                PixelCount = pixelCount;
                MinLogLuminance = -8.0f;
                LogLuminanceRange = 3.0f + 8.0f;
                Tau = 1.1f;
                Padd = default;
            }

            public LumaAvgParams(uint pixelCount, float minLogLuminance, float logLuminanceRange, float timeDelta, float tau, Vector3 padd) : this(pixelCount)
            {
                MinLogLuminance = minLogLuminance;
                LogLuminanceRange = logLuminanceRange;
                TimeDelta = timeDelta;
                Tau = tau;
                Padd = padd;
            }
        }

        #endregion Structs

        public override void Initialize(IGraphicsDevice device, PostFxDependencyBuilder builder, GraphResourceBuilder creator, int width, int height, ShaderMacro[] macros)
        {
            builder
                .AddSource("AutoExposure")
                .RunBefore("Compose")
                .RunAfter("TAA")
                .RunAfter("HBAO")
                .RunAfter("MotionBlur")
                .RunAfter("DepthOfField")
                .RunAfter("GodRays")
                .RunAfter("VolumetricClouds")
                .RunAfter("SSR")
                .RunAfter("SSGI")
                .RunAfter("LensFlare")
                .RunAfter("Bloom");

            this.creator = creator;

            this.width = width;
            this.height = height;

            LumaAvgParams lumaAvg;
            lumaAvg.PixelCount = (uint)(width * height);
            lumaAvg.MinLogLuminance = minLogLuminance;
            lumaAvg.LogLuminanceRange = Math.Abs(maxLogLuminance) + Math.Abs(minLogLuminance);
            lumaAvg.TimeDelta = Time.Delta;
            lumaAvg.Tau = tau;
            lumaAvg.Padd = default;

            lumaAvgParams = new(device, lumaAvg, CpuAccessFlags.Write);

            LumaParams luma;
            luma.MinLogLuminance = minLogLuminance;
            luma.OneOverLogLuminanceRange = 1.0f / lumaAvg.LogLuminanceRange;
            luma.InputWidth = (uint)width;
            luma.InputHeight = (uint)height;

            lumaParams = new(device, luma, CpuAccessFlags.Write);

            lumaCompute = device.CreateComputePipeline(new("compute/luma/shader.hlsl"));
            lumaAvgCompute = device.CreateComputePipeline(new("compute/lumaAvg/shader.hlsl"));

            histogram = new(device, 256, CpuAccessFlags.None, Format.R32Typeless, BufferUnorderedAccessViewFlags.Raw);

            lumaTex = creator.CreateTexture2D("Luma", new Texture2DDescription(Format.R32Float, 1, 1, 1, 1, BindFlags.ShaderResource | BindFlags.UnorderedAccess), false);
        }

        public override unsafe void Resize(int width, int height)
        {
            this.width = width;
            this.height = height;
            dirty = true;
        }

        public override void SetOutput(IRenderTargetView view, ITexture2D resource, Viewport viewport)
        {
        }

        public override void SetInput(IShaderResourceView view, ITexture2D resource)
        {
            Input = view;
        }

        public override unsafe void Update(IGraphicsContext context)
        {
            LumaAvgParams lumaAvg;
            lumaAvg.PixelCount = (uint)(width * height);
            lumaAvg.MinLogLuminance = minLogLuminance;
            lumaAvg.LogLuminanceRange = Math.Abs(maxLogLuminance) + Math.Abs(minLogLuminance);
            lumaAvg.TimeDelta = Time.Delta;
            lumaAvg.Tau = tau;
            lumaAvg.Padd = default;
            lumaAvgParams.Update(context, lumaAvg);

            if (dirty)
            {
                LumaParams luma;
                luma.MinLogLuminance = minLogLuminance;
                luma.OneOverLogLuminanceRange = 1.0f / lumaAvg.LogLuminanceRange;
                luma.InputWidth = (uint)width;
                luma.InputHeight = (uint)height;
                lumaParams.Update(context, luma);
                dirty = false;
            }
        }

        public override unsafe void Draw(IGraphicsContext context, GraphResourceBuilder creator)
        {
            context.CSSetShaderResource(0, Input);
            context.CSSetConstantBuffer(0, lumaParams);
            context.CSSetUnorderedAccessView((void*)histogram.UAV.NativePointer);
            lumaCompute.Dispatch(context, (uint)width / 16, (uint)height / 16, 1);
            nint* emptyUAVs = stackalloc nint[1];
            context.CSSetUnorderedAccessView(null);
            context.CSSetConstantBuffer(0, null);
            context.CSSetShaderResource(0, null);

            nint* lumaAvgUAVs = stackalloc nint[] { histogram.UAV.NativePointer, lumaTex.Value.UAV.NativePointer };
            uint* initialCount = stackalloc uint[] { uint.MaxValue, uint.MaxValue };
            context.CSSetConstantBuffer(0, lumaAvgParams);
            context.CSSetUnorderedAccessViews(2, (void**)lumaAvgUAVs, initialCount);
            lumaAvgCompute.Dispatch(context, 1, 1, 1);
            nint* emptyUAV2s = stackalloc nint[2];
            context.CSSetUnorderedAccessViews(2, (void**)emptyUAV2s, null);
            context.CSSetConstantBuffer(0, null);
        }

        protected override void DisposeCore()
        {
            lumaCompute.Dispose();
            lumaParams.Dispose();
            histogram.Dispose();

            lumaAvgCompute.Dispose();
            lumaAvgParams.Dispose();
            creator.RemoveResource("Luma");
        }
    }
}