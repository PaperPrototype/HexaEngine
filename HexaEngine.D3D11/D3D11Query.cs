﻿namespace HexaEngine.D3D11
{
    using HexaEngine.Core.Graphics;
    using Silk.NET.Direct3D11;

    public unsafe class D3D11Query : DeviceChildBase, IQuery
    {
        internal readonly ID3D11Query* query;

        public D3D11Query(ID3D11Query* query)
        {
            this.query = query;
            nativePointer = new(query);
        }

        protected override void DisposeCore()
        {
            query->Release();
        }
    }
}