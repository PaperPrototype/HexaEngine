﻿namespace HexaEngine.D3D11
{
    using HexaEngine.Core.Graphics;
    using Silk.NET.Direct3D11;
    using System;
    using Viewport = Mathematics.Viewport;

    public unsafe class GraphicsPipeline : IGraphicsPipeline
    {
        protected ID3D11VertexShader* vs;
        protected ID3D11HullShader* hs;
        protected ID3D11DomainShader* ds;
        protected ID3D11GeometryShader* gs;
        protected ID3D11PixelShader* ps;
        protected ID3D11InputLayout* layout;
        protected ID3D11RasterizerState* rasterizerState;
        protected ID3D11DepthStencilState* depthStencilState;
        protected ID3D11BlendState* blendState;
        protected readonly GraphicsPipelineDesc desc;
        protected readonly ShaderMacro[] macros;
        protected GraphicsPipelineState state = GraphicsPipelineState.Default;
        protected volatile bool initialized;
        protected bool valid;
        private bool disposedValue;
        protected readonly D3D11GraphicsDevice device;
        protected readonly InputElementDescription[]? inputElements;

        public GraphicsPipeline(D3D11GraphicsDevice device, GraphicsPipelineDesc desc)
        {
            Name = new FileInfo(desc.VertexShader ?? throw new ArgumentNullException(nameof(desc))).Directory?.Name ?? throw new ArgumentNullException(nameof(desc));
            this.device = device;
            this.desc = desc;
            macros = Array.Empty<ShaderMacro>();
            Compile();
            PipelineManager.Register(this);
            initialized = true;
        }

        public GraphicsPipeline(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, ShaderMacro[] macros)
        {
            Name = new FileInfo(desc.VertexShader ?? throw new ArgumentNullException(nameof(desc))).Directory?.Name ?? throw new ArgumentNullException(nameof(desc));
            this.device = device;
            this.desc = desc;
            this.macros = macros;
            Compile();
            PipelineManager.Register(this);
            initialized = true;
        }

        public GraphicsPipeline(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, InputElementDescription[] inputElements)
        {
            Name = new FileInfo(desc.VertexShader ?? throw new ArgumentNullException(nameof(desc))).Directory?.Name ?? throw new ArgumentNullException(nameof(desc));
            this.device = device;
            this.desc = desc;
            this.inputElements = inputElements;
            macros = Array.Empty<ShaderMacro>();
            Compile();
            PipelineManager.Register(this);
            initialized = true;
        }

        public GraphicsPipeline(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, InputElementDescription[] inputElements, ShaderMacro[] macros)
        {
            Name = new FileInfo(desc.VertexShader ?? throw new ArgumentNullException(nameof(desc))).Directory?.Name ?? throw new ArgumentNullException(nameof(desc));
            this.device = device;
            this.desc = desc;
            this.inputElements = inputElements;
            this.macros = macros;
            Compile();
            PipelineManager.Register(this);
            initialized = true;
        }

        public GraphicsPipeline(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, GraphicsPipelineState state)
        {
            Name = new FileInfo(desc.VertexShader ?? throw new ArgumentNullException(nameof(desc))).Directory?.Name ?? throw new ArgumentNullException(nameof(desc));
            this.device = device;
            this.desc = desc;
            State = state;
            macros = Array.Empty<ShaderMacro>();
            Compile();
            PipelineManager.Register(this);
            initialized = true;
        }

        public GraphicsPipeline(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, GraphicsPipelineState state, ShaderMacro[] macros)
        {
            Name = new FileInfo(desc.VertexShader ?? throw new ArgumentNullException(nameof(desc))).Directory?.Name ?? throw new ArgumentNullException(nameof(desc));
            this.device = device;
            this.desc = desc;
            State = state;
            this.macros = macros;
            Compile();
            PipelineManager.Register(this);
            initialized = true;
        }

        public GraphicsPipeline(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, GraphicsPipelineState state, InputElementDescription[] inputElements)
        {
            Name = new FileInfo(desc.VertexShader ?? throw new ArgumentNullException(nameof(desc))).Directory?.Name ?? throw new ArgumentNullException(nameof(desc));
            this.device = device;
            this.desc = desc;
            this.inputElements = inputElements;
            State = state;
            macros = Array.Empty<ShaderMacro>();
            Compile();
            PipelineManager.Register(this);
            initialized = true;
        }

        public GraphicsPipeline(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, GraphicsPipelineState state, InputElementDescription[] inputElements, ShaderMacro[] macros)
        {
            Name = new FileInfo(desc.VertexShader ?? throw new ArgumentNullException(nameof(desc))).Directory?.Name ?? throw new ArgumentNullException(nameof(desc));
            this.device = device;
            this.desc = desc;
            this.inputElements = inputElements;
            State = state;
            this.macros = macros;
            Compile();
            PipelineManager.Register(this);
            initialized = true;
        }

        public static Task<GraphicsPipeline> CreateAsync(D3D11GraphicsDevice device, GraphicsPipelineDesc desc)
        {
            return Task.Factory.StartNew(() => new GraphicsPipeline(device, desc));
        }

        public static Task<GraphicsPipeline> CreateAsync(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, ShaderMacro[] macros)
        {
            return Task.Factory.StartNew(() => new GraphicsPipeline(device, desc, macros));
        }

        public static Task<GraphicsPipeline> CreateAsync(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, InputElementDescription[] inputElements)
        {
            return Task.Factory.StartNew(() => new GraphicsPipeline(device, desc, inputElements));
        }

        public static Task<GraphicsPipeline> CreateAsync(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, InputElementDescription[] inputElements, ShaderMacro[] macros)
        {
            return Task.Factory.StartNew(() => new GraphicsPipeline(device, desc, inputElements, macros));
        }

        public static Task<GraphicsPipeline> CreateAsync(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, GraphicsPipelineState state)
        {
            return Task.Factory.StartNew(() => new GraphicsPipeline(device, desc, state));
        }

        public static Task<GraphicsPipeline> CreateAsync(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, GraphicsPipelineState state, ShaderMacro[] macros)
        {
            return Task.Factory.StartNew(() => new GraphicsPipeline(device, desc, state, macros));
        }

        public static Task<GraphicsPipeline> CreateAsync(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, GraphicsPipelineState state, InputElementDescription[] inputElements)
        {
            return Task.Factory.StartNew(() => new GraphicsPipeline(device, desc, state, inputElements));
        }

        public static Task<GraphicsPipeline> CreateAsync(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, GraphicsPipelineState state, InputElementDescription[] inputElements, ShaderMacro[] macros)
        {
            return Task.Factory.StartNew(() => new GraphicsPipeline(device, desc, state, inputElements, macros));
        }

        public string Name { get; }

        public GraphicsPipelineDesc Description => desc;

        public GraphicsPipelineState State
        {
            get => state;
            set
            {
                state = value;
                if (rasterizerState != null)
                    rasterizerState->Release();
                rasterizerState = null;
                if (depthStencilState != null)
                    depthStencilState->Release();
                depthStencilState = null;
                if (blendState != null)
                    blendState->Release();
                blendState = null;

                ID3D11RasterizerState* rs;
                var rsDesc = Helper.Convert(value.Rasterizer);
                device.Device->CreateRasterizerState(&rsDesc, &rs);
                rasterizerState = rs;

                ID3D11DepthStencilState* ds;
                var dsDesc = Helper.Convert(value.DepthStencil);
                device.Device->CreateDepthStencilState(&dsDesc, &ds);
                depthStencilState = ds;

                ID3D11BlendState* bs;
                var bsDesc = Helper.Convert(value.Blend);
                device.Device->CreateBlendState(&bsDesc, &bs);
                blendState = bs;
            }
        }

        public void Recompile()
        {
            initialized = false;

            if (vs != null)
                vs->Release();
            vs = null;
            if (hs != null)
                hs->Release();
            hs = null;
            if (ds != null)
                ds->Release();
            ds = null;
            if (gs != null)
                gs->Release();
            gs = null;
            if (ps != null)
                ps->Release();
            ps = null;
            layout->Release();
            layout = null;
            Compile(true);
            initialized = true;
        }

        private unsafe void Compile(bool bypassCache = false)
        {
            if (desc.VertexShader != null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.VertexShaderEntrypoint, desc.VertexShader, "vs_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ID3D11VertexShader* vertexShader;
                device.Device->CreateVertexShader(shader->Bytecode, shader->Length, null, &vertexShader);
                vs = vertexShader;
                // TODO: vs.DebugName = GetType().Name + nameof(vs);

                if (inputElements == null)
                {
                    var signature = ShaderCompiler.GetInputSignature(shader);
                    ID3D11InputLayout* il;
                    device.CreateInputLayoutFromSignature(shader, signature, &il);
                    layout = il;
                }
                else
                {
                    var signature = ShaderCompiler.GetInputSignature(shader);
                    ID3D11InputLayout* il;
                    InputElementDesc* descs = Alloc<InputElementDesc>(inputElements.Length);
                    Helper.Convert(inputElements, descs);
                    device.Device->CreateInputLayout(descs, (uint)inputElements.Length, (void*)signature.BufferPointer, signature.PointerSize, &il);
                    Helper.Free(descs, inputElements.Length);
                    Free(descs);
                    layout = il;
                }

                // TODO: layout.DebugName = GetType().Name + nameof(layout);

                Free(shader);
            }

            if (desc.HullShader != null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.HullShaderEntrypoint, desc.HullShader, "hs_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ID3D11HullShader* hullShader;
                device.Device->CreateHullShader(shader->Bytecode, shader->Length, null, &hullShader);
                hs = hullShader;
                // TODO: hs.DebugName = GetType().Name + nameof(hs);

                Free(shader);
            }

            if (desc.DomainShader != null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.DomainShaderEntrypoint, desc.DomainShader, "ds_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ID3D11DomainShader* domainShader;
                device.Device->CreateDomainShader(shader->Bytecode, shader->Length, null, &domainShader);
                ds = domainShader;
                // TODO: ds.DebugName = GetType().Name + nameof(hs);

                Free(shader);
            }

            if (desc.GeometryShader != null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.GeometryShaderEntrypoint, desc.GeometryShader, "gs_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ID3D11GeometryShader* geometryShader;
                device.Device->CreateGeometryShader(shader->Bytecode, shader->Length, null, &geometryShader);
                gs = geometryShader;
                // TODO: gs.DebugName = GetType().Name + nameof(gs);

                Free(shader);
            }

            if (desc.PixelShader != null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.PixelShaderEntrypoint, desc.PixelShader, "ps_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ID3D11PixelShader* pixelShader;
                device.Device->CreatePixelShader(shader->Bytecode, shader->Length, null, &pixelShader);
                ps = pixelShader;
                // TODO: ps.DebugName = GetType().Name + nameof(ps);

                Free(shader);
            }

            valid = true;
        }

        public virtual void BeginDraw(IGraphicsContext context, Viewport viewport)
        {
            if (context is not D3D11GraphicsContext contextd3d11) return;
            if (!initialized) return;
            if (!valid) return;

            ID3D11DeviceContext1* ctx = contextd3d11.DeviceContext;
            ctx->VSSetShader(vs, null, 0);
            ctx->HSSetShader(hs, null, 0);
            ctx->DSSetShader(ds, null, 0);
            ctx->GSSetShader(gs, null, 0);
            ctx->PSSetShader(ps, null, 0);

            var dViewport = Helper.Convert(viewport);
            ctx->RSSetViewports(1, &dViewport);
            ctx->RSSetState(rasterizerState);

            var factor = State.BlendFactor;
            float* fac = (float*)&factor;

            ctx->OMSetBlendState(blendState, fac, uint.MaxValue);
            ctx->OMSetDepthStencilState(depthStencilState, 0);
            ctx->IASetInputLayout(layout);
            ctx->IASetPrimitiveTopology(Helper.Convert(state.Topology));
        }

        public virtual void BeginDraw(ID3D11DeviceContext1* context, Viewport viewport)
        {
            if (!initialized) return;
            if (!valid) return;

            context->VSSetShader(vs, null, 0);
            context->HSSetShader(hs, null, 0);
            context->DSSetShader(ds, null, 0);
            context->GSSetShader(gs, null, 0);
            context->PSSetShader(ps, null, 0);

            var dViewport = Helper.Convert(viewport);
            context->RSSetViewports(1, &dViewport);
            context->RSSetState(rasterizerState);

            var factor = State.BlendFactor;
            float* fac = (float*)&factor;

            context->OMSetBlendState(blendState, fac, uint.MaxValue);
            context->OMSetDepthStencilState(depthStencilState, 0);
            context->IASetInputLayout(layout);
            context->IASetPrimitiveTopology(Helper.Convert(state.Topology));
        }

        public virtual void EndDraw(IGraphicsContext context)
        {
        }

        public void DrawInstanced(IGraphicsContext context, Viewport viewport, uint vertexCount, uint instanceCount, uint vertexOffset, uint instanceOffset)
        {
            if (!initialized) return;
            if (!valid) return;

            BeginDraw(context, viewport);
            context.DrawInstanced(vertexCount, instanceCount, vertexOffset, instanceOffset);
            EndDraw(context);
        }

        public void DrawIndexedInstanced(IGraphicsContext context, Viewport viewport, uint indexCount, uint instanceCount, uint indexOffset, int vertexOffset, uint instanceOffset)
        {
            if (!initialized) return;
            if (!valid) return;

            BeginDraw(context, viewport);
            context.DrawIndexedInstanced(indexCount, instanceCount, indexOffset, vertexOffset, instanceOffset);
            EndDraw(context);
        }

        public void DrawInstanced(IGraphicsContext context, Viewport viewport, IBuffer args, uint stride)
        {
            if (!initialized) return;
            if (!valid) return;

            BeginDraw(context, viewport);
            context.DrawInstancedIndirect(args, stride);
            EndDraw(context);
        }

        public void DrawIndexedInstancedIndirect(IGraphicsContext context, Viewport viewport, IBuffer args, uint stride)
        {
            if (!initialized) return;
            if (!valid) return;

            BeginDraw(context, viewport);
            context.DrawIndexedInstancedIndirect(args, stride);
            EndDraw(context);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                PipelineManager.Unregister(this);

                if (vs != null)
                    vs->Release();
                if (hs != null)
                    hs->Release();
                if (ds != null)
                    ds->Release();
                if (gs != null)
                    gs->Release();
                if (ps != null)
                    ps->Release();
                layout->Release();
                if (rasterizerState != null)
                    rasterizerState->Release();
                if (rasterizerState != null)
                    depthStencilState->Release();
                if (rasterizerState != null)
                    blendState->Release();
                disposedValue = true;
            }
        }

        ~GraphicsPipeline()
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