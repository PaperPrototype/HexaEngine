﻿namespace HexaEngine.Graphics.Renderers
{
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.Graphics.Buffers;
    using HexaEngine.Core.Graphics.Primitives;
    using HexaEngine.Meshes;
    using HexaEngine.Scenes.Managers;
    using HexaEngine.Weather;
    using System;
    using System.Numerics;

    public class SkyRenderer : IDisposable
    {
        private readonly Sphere cube;
        private readonly IGraphicsPipeline skybox;
        private readonly IGraphicsPipeline uniformColorSky;
        private readonly IGraphicsPipeline hoseWilkieSky;
        private readonly IGraphicsPipeline preethamSky;
        private readonly ConstantBuffer<CBWorld> worldBuffer;

        private ISamplerState samplerState;
        private Texture2D environment;

        private bool initialized;
        private bool disposedValue;

        public SkyRenderer(IGraphicsDevice device)
        {
            cube = new(device);
            worldBuffer = new(device, CpuAccessFlags.Write);

            skybox = device.CreateGraphicsPipeline(new()
            {
                VertexShader = "forward/sky/vs.hlsl",
                PixelShader = "forward/sky/skybox.hlsl",
                State = new()
                {
                    Rasterizer = RasterizerDescription.CullNone,
                    DepthStencil = DepthStencilDescription.DepthRead,
                    Blend = BlendDescription.Opaque
                }
            });
            uniformColorSky = device.CreateGraphicsPipeline(new()
            {
                VertexShader = "forward/sky/vs.hlsl",
                PixelShader = "forward/sky/uniformColorSky.hlsl",
                State = new()
                {
                    Rasterizer = RasterizerDescription.CullNone,
                    DepthStencil = DepthStencilDescription.DepthRead,
                    Blend = BlendDescription.Opaque
                }
            });
            hoseWilkieSky = device.CreateGraphicsPipeline(new()
            {
                VertexShader = "forward/sky/vs.hlsl",
                PixelShader = "forward/sky/hoseWilkieSky.hlsl",
                State = new()
                {
                    Rasterizer = RasterizerDescription.CullNone,
                    DepthStencil = DepthStencilDescription.DepthRead,
                    Blend = BlendDescription.Opaque
                }
            });
            preethamSky = device.CreateGraphicsPipeline(new()
            {
                VertexShader = "forward/sky/vs.hlsl",
                PixelShader = "forward/sky/preethamSky.hlsl",
                State = new()
                {
                    Rasterizer = RasterizerDescription.CullNone,
                    DepthStencil = DepthStencilDescription.DepthRead,
                    Blend = BlendDescription.Opaque
                }
            });
        }

        public void Initialize(Skybox skybox)
        {
            if (skybox.Environment == null)
            {
                return;
            }
            samplerState = skybox.SamplerState;
            environment = skybox.Environment;

            initialized = true;
        }

        public void Uninitialize()
        {
            initialized = false;

            samplerState = null;
            environment = null;
        }

        public void Update(IGraphicsContext context)
        {
            var camera = CameraManager.Current;
            if (camera == null)
            {
                return;
            }

            worldBuffer.Update(context, new(Matrix4x4.CreateScale(camera.Transform.Far - 1) * Matrix4x4.CreateTranslation(camera.Transform.Position)));
        }

        public void Draw(IGraphicsContext context, SkyType type)
        {
            WeatherManager.Current.SkyModel = type;
            switch (type)
            {
                case SkyType.Skybox:
                    if (!initialized)
                        return;
                    context.VSSetConstantBuffer(0, worldBuffer);
                    context.PSSetShaderResource(0, environment.SRV);
                    context.PSSetSampler(0, samplerState);
                    cube.DrawAuto(context, skybox);
                    break;

                case SkyType.UniformColor:
                    context.VSSetConstantBuffer(0, worldBuffer);

                    context.PSSetSampler(0, samplerState);
                    cube.DrawAuto(context, uniformColorSky);
                    break;

                case SkyType.HosekWilkie:
                    context.VSSetConstantBuffer(0, worldBuffer);

                    context.PSSetSampler(0, samplerState);
                    cube.DrawAuto(context, hoseWilkieSky);
                    break;

                case SkyType.Preetham:
                    if (!initialized)
                        return;
                    context.VSSetConstantBuffer(0, worldBuffer);
                    context.PSSetShaderResource(0, environment.SRV);
                    context.PSSetSampler(0, samplerState);
                    cube.DrawAuto(context, preethamSky);
                    break;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Uninitialize();
                cube.Dispose();
                skybox.Dispose();
                uniformColorSky.Dispose();
                hoseWilkieSky.Dispose();
                preethamSky.Dispose();
                worldBuffer.Dispose();
                disposedValue = true;
            }
        }

        ~SkyRenderer()
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