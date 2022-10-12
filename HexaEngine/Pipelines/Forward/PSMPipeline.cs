﻿namespace HexaEngine.Pipelines.Forward
{
    using HexaEngine.Core.Graphics;
    using HexaEngine.Graphics;

    public class PSMPipeline : Pipeline
    {
        public PSMPipeline(IGraphicsDevice device) : base(device, new()
        {
            VertexShader = "forward/psm/vs.hlsl",
            HullShader = "forward/psm/hs.hlsl",
            DomainShader = "forward/psm/ds.hlsl",
            PixelShader = "forward/psm/ps.hlsl",
        },
        new InputElementDescription[]
        {
                new("POSITION", 0, Format.RGB32Float, 0),
                new("TEXCOORD", 0, Format.RG32Float, 0),
                new("NORMAL", 0, Format.RGB32Float, 0),
                new("TANGENT", 0, Format.RGB32Float, 0),
                new("INSTANCED_MATS", 0, Format.RGBA32Float, 0, 1, InputClassification.PerInstanceData, 1),
                new("INSTANCED_MATS", 1, Format.RGBA32Float, 16, 1, InputClassification.PerInstanceData, 1),
                new("INSTANCED_MATS", 2, Format.RGBA32Float, 32, 1, InputClassification.PerInstanceData, 1),
                new("INSTANCED_MATS", 3, Format.RGBA32Float, 48, 1, InputClassification.PerInstanceData, 1),
        })
        {
            State = new()
            {
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullFront,
                Blend = BlendDescription.Opaque,
                Topology = PrimitiveTopology.PatchListWith3ControlPoints,
            };
        }

        protected override ShaderMacro[] GetShaderMacros()
        {
            return new ShaderMacro[] { new("INSTANCED", 1) };
        }
    }
}