﻿namespace HexaEngine.Lights.Types
{
    using HexaEngine.Configuration;
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.Graphics.Buffers;
    using HexaEngine.Editor.Attributes;
    using HexaEngine.Lights;
    using HexaEngine.Lights.Structs;
    using HexaEngine.Mathematics;
    using HexaEngine.Scenes;
    using Newtonsoft.Json;
    using System.Numerics;

    [EditorCategory("Lights")]
    [EditorGameObject<PointLight>("Point Light")]
    public class PointLight : Light
    {
        private ShadowAtlasRangeHandle? atlasHandle;

        public PointLight() : base()
        {
            ShadowMapNormalBias = 0;
            ShadowMapSlopeScaleDepthBias = 0;
        }

        [JsonConstructor]
        public PointLight(Vector4 color) : base(color)
        {
            ShadowMapNormalBias = 0;
            ShadowMapSlopeScaleDepthBias = 0;
        }

        [JsonIgnore]
        public override int ShadowMapSize => GraphicsSettings.GetSMSizePointLight(ShadowMapResolution);

        [JsonIgnore]
        public BoundingBox ShadowBox = new();

        [JsonIgnore]
        public override LightType LightType => LightType.Point;

        [JsonIgnore]
        public override bool HasShadowMap => atlasHandle?.IsValid ?? false;

        public override IShaderResourceView? GetShadowMap()
        {
            return null;
        }

        public override void CreateShadowMap(IGraphicsDevice device, ShadowAtlas atlas)
        {
            if (HasShadowMap)
            {
                return;
            }

            atlasHandle = atlas.AllocRange(ShadowMapSize, 2);
        }

        public override void DestroyShadowMap()
        {
            if (!HasShadowMap)
            {
                return;
            }

            atlasHandle.Dispose();
        }

        public unsafe void UpdateShadowBuffer(StructuredUavBuffer<ShadowData> buffer)
        {
            if (!HasShadowMap)
            {
                return;
            }

            var data = buffer.Local + QueueIndex;
            data->Size = ShadowMapSize;
            data->Softness = ShadowMapLightBleedingReduction;
            var views = ShadowData.GetViews(data);
            var coords = ShadowData.GetAtlasCoords(data);

            float texel = 1.0f / atlasHandle.Atlas.Size;

            for (int i = 0; i < 2; i++)
            {
                var vp = atlasHandle.Handles[i].Viewport;
                coords[i] = new Vector4(vp.X + 0, vp.Y + 0, vp.Width - 0, vp.Height - 0) * texel;
            }

            DPSMHelper.GetLightSpaceMatrices(Transform, Range, views, ref ShadowBox);
        }

        public unsafe Viewport UpdateShadowMap(IGraphicsContext context, StructuredUavBuffer<ShadowData> buffer, ConstantBuffer<DPSMShadowParams> osmBuffer, int pass)
        {
            if (!HasShadowMap)
            {
                return default;
            }

#nullable disable

            var data = buffer.Local + QueueIndex;
            data->Size = ShadowMapSize;
            data->Softness = 1;
            var views = ShadowData.GetViews(data);
            var coords = ShadowData.GetAtlasCoords(data);

            float texel = 1.0f / atlasHandle.Atlas.Size;

            for (int i = 0; i < 2; i++)
            {
                var vp = atlasHandle.Handles[i].Viewport;
                coords[i] = new Vector4(vp.X + 0, vp.Y + 0, vp.Width - 0, vp.Height - 0) * texel;
            }

            DPSMHelper.GetLightSpaceMatrices(Transform, Range, views, ref ShadowBox);

            var viewport = atlasHandle.Handles[pass].Viewport;
            osmBuffer.Local->View = views[0];
            osmBuffer.Local->Near = DPSMHelper.ZNear;
            osmBuffer.Local->Far = Range;
            osmBuffer.Local->HemiDir = pass == 0 ? 1 : -1;
            osmBuffer.Update(context);

            return viewport;
#nullable enable
        }

        public override unsafe bool IntersectFrustum(BoundingBox box)
        {
            return ShadowBox.Intersects(box);
        }

        public override bool UpdateShadowMapSize(Camera camera, ShadowAtlas atlas)
        {
            return false;
        }
    }
}