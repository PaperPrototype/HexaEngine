﻿namespace HexaEngine.Editor.NodeEditor
{
    public enum PinType
    {
        DontCare,
        Flow,
        Bool,
        Bool2,
        Bool3,
        Bool4,
        Bool2OrBool,
        Bool3OrBool,
        Bool4OrBool,
        AnyBool,
        Int,
        Int2,
        Int3,
        Int4,
        Int2OrInt,
        Int3OrInt,
        Int4OrInt,
        AnyInt,
        UInt,
        UInt2,
        UInt3,
        UInt4,
        UInt2OrUInt,
        UInt3OrUInt,
        UInt4OrUInt,
        AnyUInt,
        Half,
        Half2,
        Half3,
        Half4,
        Half2OrHalf,
        Half3OrHalf,
        Half4OrHalf,
        AnyHalf,
        Float,
        Float2,
        Float3,
        Float4,
        Float2OrFloat,
        Float3OrFloat,
        Float4OrFloat,
        AnyFloat,
        Double,
        Double2,
        Double3,
        Double4,
        Double2OrDouble,
        Double3OrDouble,
        Double4OrDouble,
        AnyDouble,
        String,
        Object,
        Function,
        Delegate,
        TextureAny,
        Texture1D,
        Texture1DArray,
        Texture2D,
        Texture2DMS,
        Texture2DArray,
        Texture2DMSArray,
        TextureCube,
        TextureCubeArray,
        Texture3D,
        ShaderResourceView,
        RenderTargetView,
        ConstantBuffer,
        Vertices,
        Buffer,
        Sampler,
    }

    [Flags]
    public enum PinFlags
    {
        None = 0,
        ColorEdit = 1,
        ColorPicker = 2,
        Slider = 4,
        AllowOutput = 8
    }
}