﻿namespace HexaEngine.Scenes.Managers
{
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.Graphics.Buffers;
    using HexaEngine.Core.Graphics.Primitives;
    using HexaEngine.Core.Lights;
    using HexaEngine.Core.Resources;
    using HexaEngine.Core.Scenes;
    using HexaEngine.Mathematics;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Numerics;

    public partial class LightManager
    {
        private readonly List<Light> lights = new();
        private readonly List<Light> activeLights = new();
        private readonly IGraphicsDevice device;
        private readonly IInstanceManager instanceManager;
        private readonly ConcurrentQueue<Light> updateQueue = new();
        private readonly ConcurrentQueue<ModelInstance> modelUpdateQueue = new();

        private readonly StructuredUavBuffer<DirectionalLightData> directionalLights;
        private readonly StructuredUavBuffer<PointLightData> pointLights;
        private readonly StructuredUavBuffer<SpotlightData> spotlights;

        private readonly StructuredUavBuffer<ShadowDirectionalLightData> shadowDirectionalLights;
        private readonly StructuredUavBuffer<ShadowPointLightData> shadowPointLights;
        private readonly StructuredUavBuffer<ShadowSpotlightData> shadowSpotlights;

        private readonly ConstantBuffer<LightBufferParams> paramsBuffer;

        private readonly Quad quad;
        private readonly ISamplerState pointSampler;
        private readonly ISamplerState anisoSampler;
        private readonly unsafe void** cbs;
        private readonly unsafe void** smps;

        private readonly IGraphicsPipeline deferredDirect;
        private readonly unsafe void** directSrvs;
        private const uint ndirectSrvs = 8 + 3;

        private readonly IGraphicsPipeline deferredIndirect;
        private readonly unsafe void** indirectSrvs;
        private const uint nindirectSrvs = 8 + 4;

        private readonly IGraphicsPipeline deferredShadow;
        private readonly unsafe void** shadowSrvs;
        private const uint nShadowSrvs = 8 + 3 + MaxDirectionalLightSDs + MaxPointLightSDs + MaxSpotlightSDs;

        public const int MaxDirectionalLightSDs = 1;
        public const int MaxPointLightSDs = 32;
        public const int MaxSpotlightSDs = 32;

        public IRenderTargetView Output;

        public IShaderResourceView[] GBuffers;

        public IShaderResourceView Irraidance;
        public IShaderResourceView EnvPrefiltered;
        public IShaderResourceView LUT;
        public IShaderResourceView SSAO;

        public IShaderResourceView[] CSMs;
        public IShaderResourceView[] OSMs;
        public IShaderResourceView[] PSMs;
        public IBuffer Camera;

        public LightManager(IGraphicsDevice device, IInstanceManager instanceManager)
        {
            this.device = device;
            this.instanceManager = instanceManager;
            instanceManager.Updated += InstanceUpdated;
            instanceManager.InstanceCreated += InstanceCreated;
            instanceManager.InstanceDestroyed += InstanceDestroyed;
            paramsBuffer = new(device, CpuAccessFlags.Write);
            directionalLights = new(device, true, false);
            pointLights = new(device, true, false);
            spotlights = new(device, true, false);

            shadowDirectionalLights = new(device, true, false);
            shadowPointLights = new(device, true, false);
            shadowSpotlights = new(device, true, false);

            quad = new(device);
            deferredDirect = device.CreateGraphicsPipeline(new()
            {
                VertexShader = "deferred/brdf/vs.hlsl",
                PixelShader = "deferred/brdf/direct.hlsl",
            },
            new GraphicsPipelineState()
            {
                Blend = BlendDescription.Additive,
                BlendFactor = Vector4.One,
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullBack,
                Topology = PrimitiveTopology.TriangleList
            });

            pointSampler = ResourceManager.GetOrAddSamplerState("PointClamp", SamplerDescription.PointClamp);
            anisoSampler = ResourceManager.GetOrAddSamplerState("AnisotropicClamp", SamplerDescription.AnisotropicClamp);

            unsafe
            {
                smps = AllocArray(2);
                smps[0] = (void*)pointSampler.NativePointer;
                smps[1] = (void*)anisoSampler.NativePointer;
                directSrvs = AllocArray(ndirectSrvs);
                cbs = AllocArray(2);

                indirectSrvs = AllocArray(nindirectSrvs);
                shadowSrvs = AllocArray(nShadowSrvs);
                Zero(shadowSrvs, (uint)(nShadowSrvs * sizeof(nint)));
            }

            deferredIndirect = device.CreateGraphicsPipeline(new()
            {
                VertexShader = "deferred/brdf/vs.hlsl",
                PixelShader = "deferred/brdf/indirect.hlsl",
            },
            new GraphicsPipelineState()
            {
                Blend = BlendDescription.Additive,
                BlendFactor = Vector4.One,
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullBack,
                Topology = PrimitiveTopology.TriangleList
            });

            deferredShadow = device.CreateGraphicsPipeline(new()
            {
                VertexShader = "deferred/brdf/vs.hlsl",
                PixelShader = "deferred/brdf/shadow.hlsl",
            },
            new GraphicsPipelineState()
            {
                Blend = BlendDescription.Additive,
                BlendFactor = Vector4.One,
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullBack,
                Topology = PrimitiveTopology.TriangleList
            });
        }

        public IReadOnlyList<Light> Lights => lights;

        public IReadOnlyList<Light> Active => activeLights;

        public int Count => lights.Count;

        public int ActiveCount => activeLights.Count;

        private void InstanceUpdated(ModelInstanceType type, ModelInstance instance)
        {
            modelUpdateQueue.Enqueue(instance);
        }

        private void InstanceDestroyed(ModelInstance instance)
        {
            modelUpdateQueue.Enqueue(instance);
        }

        private void InstanceCreated(ModelInstance instance)
        {
            modelUpdateQueue.Enqueue(instance);
        }

        private void LightTransformed(GameObject light)
        {
            updateQueue.Enqueue((Light)light);
        }

        private void LightPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is Light light)
                updateQueue.Enqueue(light);
        }

        public void Clear()
        {
            lock (lights)
            {
                lights.Clear();
            }
        }

        public unsafe void Register(GameObject gameObject)
        {
            if (gameObject is Light light)
            {
                AddLight(light);
            }
        }

        public unsafe void Unregister(GameObject gameObject)
        {
            if (gameObject is Light light)
            {
                light.DestroyShadowMap();
                RemoveLight(light);
            }
        }

        public unsafe void AddLight(Light light)
        {
            lock (lights)
            {
                lights.Add(light);
                updateQueue.Enqueue(light);
                light.Transformed += LightTransformed;
                light.PropertyChanged += LightPropertyChanged;
            }
        }

        public unsafe void RemoveLight(Light light)
        {
            lock (lights)
            {
                light.PropertyChanged -= LightPropertyChanged;
                light.Transformed -= LightTransformed;
                lights.Remove(light);
                activeLights.Remove(light);
            }
        }

        public unsafe void Update(IGraphicsContext context, Camera camera)
        {
            Queue<Light> updateShadowLightQueue = new();
            while (updateQueue.TryDequeue(out var light))
            {
                if (light.IsEnabled)
                {
                    if (!activeLights.Contains(light))
                    {
                        activeLights.Add(light);
                    }

                    if (light.CastShadows)
                    {
                        light.CreateShadowMap(context.Device);
                    }
                    else
                    {
                        light.DestroyShadowMap();
                    }

                    updateShadowLightQueue.Enqueue(light);
                }
                else
                {
                    activeLights.Remove(light);
                }
            }

            UpdateDirectLights(context);

            while (modelUpdateQueue.TryDequeue(out var instance))
            {
                instance.GetBoundingBox(out BoundingBox box);
                for (int i = 0; i < activeLights.Count; i++)
                {
                    var light = activeLights[i];
                    if (!light.CastShadows)
                        continue;
                    if (light.IntersectFrustum(box) && !updateShadowLightQueue.Contains(light))
                    {
                        updateShadowLightQueue.Enqueue(light);
                    }
                }
            }

            UpdateShadowMaps(context, camera, updateShadowLightQueue);
        }

        public void BeginResize()
        {
            ResourceManager.RequireUpdate("LightBuffer");
        }

        public async void EndResize(int width, int height)
        {
            Output = ResourceManager.UpdateTextureRTV("LightBuffer", TextureDescription.CreateTexture2DWithRTV(width, height, 1));

            Camera = await ResourceManager.GetConstantBufferAsync("CBCamera");
            GBuffers = await ResourceManager.GetTextureArraySRVAsync("GBuffer");
            Irraidance = await ResourceManager.GetTextureSRVAsync("EnvironmentIrradiance");
            EnvPrefiltered = await ResourceManager.GetTextureSRVAsync("EnvironmentPrefilter");
            LUT = await ResourceManager.GetTextureSRVAsync("BRDFLUT");
            SSAO = await ResourceManager.GetTextureSRVAsync("SSAOBuffer");
            UpdateResources();
        }

        public void UpdateTextures()
        {
            Irraidance = ResourceManager.GetTextureSRV("EnvironmentIrradiance");
            EnvPrefiltered = ResourceManager.GetTextureSRV("EnvironmentPrefilter");
            UpdateResources();
        }

        private unsafe void UpdateResources()
        {
            cbs[0] = (void*)paramsBuffer.Buffer?.NativePointer;
            cbs[1] = (void*)Camera?.NativePointer;

            if (GBuffers != null)
                for (int i = 0; i < 8; i++)
                {
                    if (i < GBuffers.Length)
                    {
                        shadowSrvs[i] = indirectSrvs[i] = directSrvs[i] = (void*)GBuffers[i]?.NativePointer;
                    }
                }
            directSrvs[8] = (void*)directionalLights.SRV.NativePointer;
            directSrvs[9] = (void*)pointLights.SRV.NativePointer;
            directSrvs[10] = (void*)spotlights.SRV.NativePointer;

            shadowSrvs[8] = (void*)shadowDirectionalLights.SRV.NativePointer;
            shadowSrvs[9] = (void*)shadowPointLights.SRV.NativePointer;
            shadowSrvs[10] = (void*)shadowSpotlights.SRV.NativePointer;

            indirectSrvs[8] = (void*)Irraidance?.NativePointer;
            indirectSrvs[9] = (void*)EnvPrefiltered?.NativePointer;
            indirectSrvs[10] = (void*)LUT?.NativePointer;
            indirectSrvs[11] = (void*)SSAO?.NativePointer;
        }

        private unsafe void UpdateDirectLights(IGraphicsContext context)
        {
            directionalLights.ResetCounter();
            pointLights.ResetCounter();
            spotlights.ResetCounter();
            shadowDirectionalLights.ResetCounter();
            shadowPointLights.ResetCounter();
            shadowSpotlights.ResetCounter();
            uint csmCount = 0;
            uint osmCount = 0;
            uint psmCount = 0;

            for (int i = 0; i < activeLights.Count; i++)
            {
                var light = activeLights[i];
                if (light.CastShadows)
                {
                    switch (light.LightType)
                    {
                        case LightType.Directional:
                            if (csmCount == MaxDirectionalLightSDs)
                                continue;
                            light.QueueIndex = csmCount;
                            shadowDirectionalLights.Add(new((DirectionalLight)light));
                            shadowSrvs[ndirectSrvs + csmCount] = (void*)light.GetShadowMap()?.NativePointer;
                            csmCount++;
                            break;

                        case LightType.Point:
                            if (osmCount == MaxPointLightSDs)
                                continue;
                            light.QueueIndex = osmCount;
                            shadowPointLights.Add(new((PointLight)light));
                            shadowSrvs[ndirectSrvs + MaxDirectionalLightSDs + osmCount] = (void*)light.GetShadowMap()?.NativePointer;
                            osmCount++;
                            break;

                        case LightType.Spot:
                            if (psmCount == MaxSpotlightSDs)
                                continue;
                            light.QueueIndex = psmCount;
                            shadowSpotlights.Add(new((Spotlight)light));
                            shadowSrvs[ndirectSrvs + MaxDirectionalLightSDs + +MaxPointLightSDs + psmCount] = (void*)light.GetShadowMap()?.NativePointer;
                            psmCount++;
                            break;
                    }
                }
                else
                {
                    switch (light.LightType)
                    {
                        case LightType.Directional:
                            light.QueueIndex = directionalLights.Count;
                            directionalLights.Add(new((DirectionalLight)light));
                            break;

                        case LightType.Point:
                            light.QueueIndex = pointLights.Count;
                            pointLights.Add(new((PointLight)light));
                            break;

                        case LightType.Spot:
                            light.QueueIndex = spotlights.Count;
                            spotlights.Add(new((Spotlight)light));
                            break;
                    }
                }
            }

            for (uint i = csmCount; i < MaxDirectionalLightSDs; i++)
            {
                shadowSrvs[ndirectSrvs + i] = null;
            }
            for (uint i = osmCount; i < MaxPointLightSDs; i++)
            {
                shadowSrvs[ndirectSrvs + MaxDirectionalLightSDs + i] = null;
            }
            for (uint i = psmCount; i < MaxSpotlightSDs; i++)
            {
                shadowSrvs[ndirectSrvs + MaxDirectionalLightSDs + MaxPointLightSDs + i] = null;
            }

            directionalLights.Update(context);
            pointLights.Update(context);
            spotlights.Update(context);

            shadowDirectionalLights.Update(context);
            shadowPointLights.Update(context);
            shadowSpotlights.Update(context);

            directSrvs[8] = (void*)directionalLights.SRV.NativePointer;
            directSrvs[9] = (void*)pointLights.SRV.NativePointer;
            directSrvs[10] = (void*)spotlights.SRV.NativePointer;
        }

        public unsafe void DeferredPass(IGraphicsContext context, Camera camera)
        {
            Update(context, camera);
            context.ClearRenderTargetView(Output, default);
            context.SetRenderTarget(Output, default);
            context.PSSetConstantBuffers(cbs, 2, 0);
            context.PSSetSamplers(smps, 2, 0);

            context.PSSetShaderResources(indirectSrvs, nindirectSrvs, 0);

            // Indirect light pass
            quad.DrawAuto(context, deferredIndirect, Output.Viewport);

            var lightParams = paramsBuffer.Local;
            lightParams->DirectionalLights = directionalLights.Count;
            lightParams->PointLights = pointLights.Count;
            lightParams->Spotlights = spotlights.Count;
            paramsBuffer.Update(context);

            context.PSSetShaderResources(directSrvs, ndirectSrvs, 0);

            // Direct light pass
            quad.DrawAuto(context, deferredDirect, Output.Viewport);

            lightParams->DirectionalLights = shadowDirectionalLights.Count;
            lightParams->PointLights = shadowPointLights.Count;
            lightParams->Spotlights = shadowSpotlights.Count;
            paramsBuffer.Update(context);

            context.PSSetShaderResources(shadowSrvs, nShadowSrvs, 0);

            // Shadow light pass
            quad.DrawAuto(context, deferredShadow, Output.Viewport);
        }

        public unsafe void Dispose()
        {
            instanceManager.Updated -= InstanceUpdated;

            directionalLights.Dispose();
            pointLights.Dispose();
            spotlights.Dispose();

            shadowDirectionalLights.Dispose();
            shadowPointLights.Dispose();
            shadowSpotlights.Dispose();

            paramsBuffer.Dispose();
            quad.Dispose();
            deferredDirect.Dispose();
            deferredIndirect.Dispose();
            deferredShadow.Dispose();
            Free(directSrvs);
            Free(indirectSrvs);
            Free(shadowSrvs);
            Free(smps);
            Free(cbs);
        }

        public unsafe void UpdateShadowMaps(IGraphicsContext context, Camera camera, Queue<Light> lights)
        {
            while (lights.TryDequeue(out var light))
            {
                switch (light.LightType)
                {
                    case LightType.Directional:
                        ((DirectionalLight)light).UpdateShadowMap(context, shadowDirectionalLights, camera, instanceManager);
                        break;

                    case LightType.Point:
                        ((PointLight)light).UpdateShadowMap(context, shadowPointLights, instanceManager);
                        break;

                    case LightType.Spot:
                        ((Spotlight)light).UpdateShadowMap(context, shadowSpotlights, instanceManager);
                        break;
                }
            }
        }
    }
}