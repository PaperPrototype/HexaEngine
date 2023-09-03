﻿namespace HexaEngine.Core.Graphics.Primitives
{
    using HexaEngine.Core.Graphics;
    using System;

    public interface IPrimitive : IDisposable
    {
        void DrawAuto(IGraphicsContext context, IGraphicsPipeline pipeline);

        void DrawAuto(IGraphicsContext context);

        void Bind(IGraphicsContext context, out uint vertexCount, out uint indexCount, out int instanceCount);

        void Unbind(IGraphicsContext context);
    }
}