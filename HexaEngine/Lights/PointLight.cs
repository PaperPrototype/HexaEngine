﻿namespace HexaEngine.Lights
{
    using HexaEngine.Editor.Attributes;
    using HexaEngine.Mathematics;

    [EditorNode<PointLight>("Point Light")]
    public class PointLight : Light
    {
        [JsonIgnore]
        public readonly unsafe BoundingBox* ShadowBox;

        public unsafe PointLight()
        {
            Transform.Updated += (s, e) => { Updated = true; };
            ShadowBox = Alloc<BoundingBox>();
        }

        [EditorProperty("Shadow Range")]
        public float ShadowRange { get; set; } = 100;

        [EditorProperty("Strength")]
        public float Strength { get; set; } = 1000;

        [JsonIgnore]
        public override LightType Type => LightType.Point;

        public override unsafe void Uninitialize()
        {
            base.Uninitialize();
            Free(ShadowBox);
        }
    }
}