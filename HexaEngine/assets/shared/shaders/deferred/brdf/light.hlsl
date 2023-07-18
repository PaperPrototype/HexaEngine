#include "../../gbuffer.hlsl"
#include "../../camera.hlsl"
#include "../../light.hlsl"
#include "../../common.hlsl"
#include "../../shadow.hlsl"

#ifndef CLUSTERED_DEFERRED
#define CLUSTERED_DEFERRED 0
#endif

#if CLUSTERED_DEFERRED
#include "../../cluster.hlsl"
#endif

Texture2D GBufferA : register(t0);
Texture2D GBufferB : register(t1);
Texture2D GBufferC : register(t2);
Texture2D GBufferD : register(t3);
Texture2D<float> Depth : register(t4);
Texture2D ssao : register(t5);
StructuredBuffer<Light> lights : register(t6);
StructuredBuffer<ShadowData> shadowData : register(t7);

#if !CLUSTERED_DEFERRED
Texture2D depthAtlas : register(t8);
Texture2DArray depthCSM : register(t9);
#endif

#if CLUSTERED_DEFERRED
StructuredBuffer<uint> lightIndexList : register(t8); //MAX_CLUSTER_LIGHTS * 16^3
StructuredBuffer<LightGrid> lightGrid : register(t9); //16^3
Texture2D depthAtlas : register(t10);
Texture2DArray depthCSM : register(t11);
#endif

#if !CLUSTERED_DEFERRED
cbuffer constants : register(b0)
{
    uint cLightCount;
};
#endif

SamplerState linearClampSampler : register(s0);
SamplerState linearWrapSampler : register(s1);
SamplerState pointClampSampler : register(s2);
SamplerComparisonState shadowSampler : register(s3);

struct VSOut
{
    float4 Pos : SV_Position;
    float2 Tex : TEXCOORD;
};

/*
float InterleavedGradientNoise(float2 position_screen)
{
    position_screen += g_frame * any(g_taa_jitter_offset); // temporal factor
    float3 magic = float3(0.06711056f, 0.00583715f, 52.9829189f);
    return frac(magic.z * frac(dot(position_screen, magic.xy)));
}
*/

//https://panoskarabelas.com/posts/screen_space_shadows/
static const uint SSS_MAX_STEPS = 16; // Max ray steps, affects quality and performance.
static const float SSS_RAY_MAX_DISTANCE = 0.05f; // Max shadow length, longer shadows are less accurate.
static const float SSS_THICKNESS = 0.5f; // Depth testing thickness.
static const float SSS_STEP_LENGTH = SSS_RAY_MAX_DISTANCE / (float) SSS_MAX_STEPS;

float SSCS(float3 position, float2 uv, float3 direction)
{
    // Compute ray position and direction (in view-space)
    float3 ray_pos = mul(float4(position, 1.0f), view).xyz;
    float3 ray_dir = mul(float4(-direction, 0.0f), view).xyz;

    // Compute ray step
    float3 ray_step = ray_dir * SSS_STEP_LENGTH;

    //float offset = InterleavedGradientNoise(screen_dim * uv) * 2.0f - 1.0f;
    //ray_pos += ray_step * offset;

    // Ray march towards the light
    float occlusion = 0.0;
    float2 ray_uv = 0.0f;
    for (uint i = 0; i < SSS_MAX_STEPS; i++)
    {
        // Step the ray
        ray_pos += ray_step;
        ray_uv = ProjectUV(ray_pos, proj);

        // Ensure the UV coordinates are inside the screen
        if (IsSaturated(ray_uv))
        {
            // Compute the difference between the ray's and the camera's depth
            float depth_z = SampleLinearDepth(Depth, linearClampSampler, ray_uv);
            float depth_delta = ray_pos.z - depth_z;

            // Check if the camera can't "see" the ray (ray depth must be larger than the camera depth, so positive depth_delta)
            if ((depth_delta > 0.0f) && (depth_delta < SSS_THICKNESS))
            {
                // Mark as occluded
                occlusion = 1.0f;

                // Fade out as we approach the edges of the screen
                occlusion *= ScreenFade(ray_uv);

                break;
            }
        }
    }

    // Convert to visibility
    return 1.0f - occlusion;
}

float ShadowFactorSpotlight(ShadowData data, float3 position, SamplerComparisonState state)
{
    float3 uvd = GetShadowAtlasUVD(position, data.size, data.offsets[0], data.views[0]);

#if HARD_SHADOWS_SPOTLIGHTS
    return CalcShadowFactor_Basic(state, depthAtlas, uvd);
#else
    return CalcShadowFactor_PCF3x3(state, depthAtlas, uvd, data.size, data.softness);
#endif
}

int CubeFaceFromDirection(float3 direction)
{
    float3 absDirection = abs(direction);
    int faceIndex = 0;

    if (absDirection.x >= absDirection.y && absDirection.x >= absDirection.z)
    {
        faceIndex = (direction.x > 0.0) ? 0 : 1; // Positive X face (0), Negative X face (1)
    }
    else if (absDirection.y >= absDirection.x && absDirection.y >= absDirection.z)
    {
        faceIndex = (direction.y > 0.0) ? 2 : 3; // Positive Y face (2), Negative Y face (3)
    }
    else
    {
        faceIndex = (direction.z > 0.0) ? 4 : 5; // Positive Z face (4), Negative Z face (5)
    }

    return faceIndex;
}

#define HARD_SHADOWS_POINTLIGHTS 1
float ShadowFactorPointLight(ShadowData data, Light light, float3 position, SamplerComparisonState state)
{
    float3 light_to_pixelWS = position - light.position.xyz;
    float depthValue = length(light_to_pixelWS) / light.range;

    int face = CubeFaceFromDirection(normalize(light_to_pixelWS.xyz));
    float3 uvd = GetShadowAtlasUVD(position, data.size, data.offsets[face], data.views[face]);
    uvd.z = depthValue;

#if HARD_SHADOWS_POINTLIGHTS
    return CalcShadowFactor_Basic(state, depthAtlas, uvd);
#else
    return CalcShadowFactor_PCF3x3(state, depthAtlas, uvd, data.size, data.softness);
#endif
}

float ShadowFactorDirectionalLight(ShadowData data, float3 position, Texture2D depthTex, SamplerComparisonState state)
{
    float3 uvd = GetShadowUVD(position, data.views[0]);

#if HARD_SHADOWS_DIRECTIONAL
    return CalcShadowFactor_Basic(state, depthTex, uvd);
#else
    return CalcShadowFactor_PCF3x3(state, depthTex, uvd, data.size, 1);
#endif
}

float ShadowFactorDirectionalLightCascaded(ShadowData data, float camFar, float4x4 camView, float3 position, Texture2DArray depthTex, SamplerComparisonState state)
{
    float cascadePlaneDistances[8] = (float[8]) data.cascades;
    float farPlane = camFar;

	// select cascade layer
    float4 fragPosViewSpace = mul(float4(position, 1.0), camView);
    float depthValue = abs(fragPosViewSpace.z);
    float cascadePlaneDistance;
    uint layer = data.cascadeCount;
    for (uint i = 0; i < data.cascadeCount; ++i)
    {
        if (depthValue < cascadePlaneDistances[i])
        {
            cascadePlaneDistance = cascadePlaneDistances[i];
            layer = i;
            break;
        }
    }

    float3 uvd = GetShadowUVD(position, data.views[layer]);

#if HARD_SHADOWS_DIRECTIONAL_CASCADED
    return CSMCalcShadowFactor_Basic(state, depthTex, layer, uvd, data.size, data.softness);
#else
    return CSMCalcShadowFactor_PCF3x3(state, depthTex, layer, uvd, data.size, data.softness);
#endif
}

#if CLUSTERED_DEFERRED
float4 ComputeLightingPBR(VSOut input, float3 position, const uint tileIndex, GeometryAttributes attrs)
#else
float4 ComputeLightingPBR(VSOut input, float3 position, GeometryAttributes attrs)
#endif
{
#if CLUSTERED_DEFERRED
    uint lightCount = lightGrid[tileIndex].lightCount;
    uint lightOffset = lightGrid[tileIndex].lightOffset;
#else
    uint lightCount = cLightCount;
#endif

    float3 baseColor = attrs.baseColor;
    float roughness = attrs.roughness;
    float metallic = attrs.metallic;

    float3 N = normalize(attrs.normal);
    float3 V = normalize(GetCameraPos() - position);

    float3 F0 = lerp(float3(0.04f, 0.04f, 0.04f), baseColor, metallic);

    float3 Lo = float3(0, 0, 0);

    [loop]
    for (uint i = 0; i < lightCount; i++)
    {
        float3 L = 0;
#if CLUSTERED_DEFERRED
        uint lightIndex = lightIndexList[lightOffset + i];
        Light light = lights[lightIndex];
#else
        Light light = lights[i];
#endif

        switch (light.type)
        {
            case POINT_LIGHT:
                L += PointLightBRDF(light, position, F0, V, N, baseColor, roughness, metallic);
                break;
            case SPOT_LIGHT:
                L += SpotlightBRDF(light, position, F0, V, N, baseColor, roughness, metallic);
                break;
            case DIRECTIONAL_LIGHT:
                L += DirectionalLightBRDF(light, F0, V, N, baseColor, roughness, metallic);
                break;
        }

        float shadowFactor = 1;

        if (light.castsShadows)
        {
#if CLUSTERED_DEFERRED
            ShadowData data = shadowData[lightIndex];
#else
            ShadowData data = shadowData[i];
#endif
            switch (light.type)
            {
                case POINT_LIGHT:
                    shadowFactor = ShadowFactorPointLight(data, light, position, shadowSampler);
                    break;
                case SPOT_LIGHT:
                    shadowFactor = ShadowFactorSpotlight(data, position, shadowSampler);
                    break;
                case DIRECTIONAL_LIGHT:
                    shadowFactor = ShadowFactorDirectionalLightCascaded(data, camFar, view, position, depthCSM, shadowSampler);
                    break;
            }
        }

        Lo += L * shadowFactor;
    }

    float ao = ssao.Sample(linearWrapSampler, input.Tex).r * attrs.ao;
    return float4(Lo * ao, 1);
}

float4 main(VSOut pixel) : SV_TARGET
{
    float depth = Depth.Sample(linearWrapSampler, pixel.Tex);
    float3 position = GetPositionWS(pixel.Tex, depth);
    GeometryAttributes attrs;
    ExtractGeometryData(pixel.Tex, GBufferA, GBufferB, GBufferC, GBufferD, linearWrapSampler, attrs);

#if CLUSTERED_DEFERRED
    uint tileIndex = GetClusterIndex(depth, camNear, camFar, screenDim, pixel.Pos);
    return ComputeLightingPBR(pixel, position, tileIndex, attrs);
#else
    return ComputeLightingPBR(pixel, position, attrs);
#endif
}