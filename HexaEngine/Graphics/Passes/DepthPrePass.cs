﻿namespace HexaEngine.Graphics.Passes
{
    using HexaEngine.Core.Debugging;
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.Graphics.Buffers;
    using HexaEngine.Graphics.Culling;
    using HexaEngine.Graphics.Graph;
    using HexaEngine.Meshes;
    using HexaEngine.Scenes;

    public class DepthPrePass : RenderPass
    {
        private ResourceRef<DepthStencil> depthStencil;
        private ResourceRef<ConstantBuffer<CBCamera>> camera;

        public DepthPrePass() : base("PrePass")
        {
            AddWriteDependency(new("#DepthStencil"));
        }

        public override void Init(GraphResourceBuilder creator, ICPUProfiler? profiler)
        {
            depthStencil = creator.GetDepthStencilBuffer("#DepthStencil");
            camera = creator.GetConstantBuffer<CBCamera>("CBCamera");
            // force init.
            _ = CullingManager.Current;
        }

        public override void Release()
        {
            CullingManager.Current.Release();
        }

        public override void Execute(IGraphicsContext context, GraphResourceBuilder creator, ICPUProfiler? profiler)
        {
            var current = SceneManager.Current;
            if (current == null)
            {
                return;
            }

            var renderers = current.RenderManager;

            var culling = CullingManager.Current;

            culling.Context.Reset();

            profiler?.Begin("VisibilityTest.Geometry");
            RenderManager.ExecuteGroupVisibilityTest(renderers.GeometryQueue, CullingManager.Current.Context, profiler, "Geometry");
            profiler?.End("VisibilityTest.Geometry");

            profiler?.Begin("VisibilityTest.AlphaTest");
            RenderManager.ExecuteGroupVisibilityTest(renderers.AlphaTestQueue, CullingManager.Current.Context, profiler, "AlphaTest");
            profiler?.End("VisibilityTest.AlphaTest");

            culling.Context.Flush(context);

            var depthStencilBuffer = depthStencil.Value;
            context.ClearDepthStencilView(depthStencilBuffer.DSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
            context.SetRenderTarget(null, depthStencilBuffer.DSV);

            context.SetViewport(depthStencilBuffer.Viewport);
            context.VSSetConstantBuffer(1, camera.Value);
            context.DSSetConstantBuffer(1, camera.Value);
            context.GSSetConstantBuffer(1, camera.Value);
            context.CSSetConstantBuffer(1, camera.Value);

            profiler?.Begin("PrePass.Geometry");
            RenderManager.ExecuteGroupDepth(renderers.GeometryQueue, context, profiler, "Geometry");
            profiler?.End("PrePass.Geometry");

            profiler?.Begin("PrePass.AlphaTest");
            RenderManager.ExecuteGroupDepth(renderers.AlphaTestQueue, context, profiler, "AlphaTest");
            profiler?.End("PrePass.AlphaTest");

            context.ClearState();
        }
    }
}