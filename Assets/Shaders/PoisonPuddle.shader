Shader "Game/Poison Puddle"
{
    Properties
    {
        _BaseColor ("Poison Color", Color) = (0.12, 0.32, 0.025, 0.35)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 discPosition : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.discPosition = input.positionOS.xz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float radius = length(input.discPosition);
                float feather = 1.0 - smoothstep(0.7, 1.0, radius);
                float ripples = 0.85 + 0.15 * sin(radius * 24.0 + input.discPosition.x * 5.0);
                return half4(_BaseColor.rgb * ripples, _BaseColor.a * feather);
            }
            ENDHLSL
        }
    }
}
