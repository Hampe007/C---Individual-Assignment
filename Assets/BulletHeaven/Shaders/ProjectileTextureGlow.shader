Shader "BulletHeaven/Projectile Texture Glow"
{
    Properties
    {
        [MainTexture] _BaseMap ("Surface Pattern", 2D) = "white" {}
        [MainColor][HDR] _BaseColor ("Glow Color", Color) = (1, 1, 1, 1)
        _DetailStrength ("Pattern Strength", Range(0, 1)) = 1
        _GlowIntensity ("Surface Brightness", Range(0, 4)) = 1
        _RimIntensity ("Edge Glow", Range(0, 8)) = 1.5
        _RimPower ("Edge Sharpness", Range(1, 10)) = 5
        _PulseStrength ("Pulse Strength", Range(0, 0.3)) = 0.08
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _DetailStrength;
                half _GlowIntensity;
                half _RimIntensity;
                half _RimPower;
                half _PulseStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half3 pattern = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half facing = saturate(dot(normalize(input.normalWS), viewDirection));
                half rim = pow(1.0 - facing, _RimPower);
                half pulse = 1.0 + sin(_Time.y * 4.0) * _PulseStrength;
                // Retain the full texture and its dark details rather than reducing it
                // to a narrow brightness range on top of an already overexposed color.
                // Lift the texture's dark midtones without adding a flat emissive floor.
                half3 surface = lerp(half3(1, 1, 1), sqrt(max(pattern, half3(0, 0, 0))), _DetailStrength);
                half3 tint = lerp(half3(1, 1, 1), _BaseColor.rgb, 0.65);
                surface *= tint * _GlowIntensity * lerp(0.65, 1.0, facing);
                // Only the narrow silhouette is HDR; the textured face stays readable.
                half3 edgeGlow = _BaseColor.rgb * rim * _RimIntensity * pulse;
                return half4(surface + edgeGlow, 1);
            }
            ENDHLSL
        }
    }
}
