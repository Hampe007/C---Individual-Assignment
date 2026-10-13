Shader "BulletHeaven/Soft Projectile Particles"
{
    Properties
    {
        [HDR] _BaseColor ("Glow Color", Color) = (1, 1, 1, 1)
        _Intensity ("Glow Intensity", Range(0, 8)) = 2.5
        [Toggle] _Trail ("Trail Shape", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }
        Pass
        {
            Name "SoftGlow"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Intensity;
                half _Trail;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _BaseColor;
                return output;
            }
            half4 Fragment(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float radius = lerp(length(centered), abs(centered.y), _Trail);
                // Fully transparent billboard edges; no square silhouette or texture dependency.
                half softEdge = 1.0 - smoothstep(0.15, 1.0, radius);
                half core = exp2(-radius * radius * 12.0);
                half3 color = input.color.rgb * _Intensity * (0.65 + core * 0.6);
                return half4(color, input.color.a * softEdge * softEdge);
            }
            ENDHLSL
        }
    }
}
