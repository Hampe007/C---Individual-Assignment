Shader "BulletHeaven/Projectile Texture Glow"
{
    Properties
    {
        [MainTexture] _BaseMap ("Surface Pattern", 2D) = "white" {}
        [MainColor][HDR] _BaseColor ("Glow Color", Color) = (1, 1, 1, 1)
        _DetailStrength ("Pattern Strength", Range(0, 1)) = 0.5
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
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half3 pattern = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half luminance = dot(pattern, half3(0.2126, 0.7152, 0.0722));
                half variation = lerp(0.55, 1.45, saturate(luminance));
                half brightness = lerp(1.0, variation, _DetailStrength);
                return half4(_BaseColor.rgb * brightness, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
