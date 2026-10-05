Shader "Game/UI/WetLensBlood"
{
    Properties
    {
        [PerRendererData] _MainTex ("Splat Atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Wetness ("Wet Highlights", Range(0,1)) = 0.45
        _Thickness ("Pooled Darkness", Range(0,1)) = 0.45
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct Input { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float4 localPosition : TEXCOORD1; float4 screenPosition : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float4 _ClipRect;
            float _Wetness;
            float _Thickness;

            Output Vertex(Input input)
            {
                Output output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.localPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.screenPosition = ComputeScreenPos(output.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            float Grain(float2 position)
            {
                float2 cell = floor(position);
                float2 blend = frac(position);
                blend = blend * blend * (3.0 - 2.0 * blend);
                float4 samples = frac(sin(float4(dot(cell, float2(127.1,311.7)), dot(cell+float2(1,0), float2(127.1,311.7)), dot(cell+float2(0,1), float2(127.1,311.7)), dot(cell+1, float2(127.1,311.7)))) * 43758.5453);
                return lerp(lerp(samples.x, samples.y, blend.x), lerp(samples.z, samples.w, blend.x), blend.y);
            }

            float4 Fragment(Output input) : SV_Target
            {
                float alpha = tex2D(_MainTex, input.uv).a;
                float2 stepSize = _MainTex_TexelSize.xy * 1.5;
                float2 slope = float2(tex2D(_MainTex, input.uv + float2(stepSize.x,0)).a - tex2D(_MainTex, input.uv - float2(stepSize.x,0)).a,
                    tex2D(_MainTex, input.uv + float2(0,stepSize.y)).a - tex2D(_MainTex, input.uv - float2(0,stepSize.y)).a);
                float2 screenUv = input.screenPosition.xy / input.screenPosition.w;
                float pooling = Grain(screenUv * float2(13,9)) * 0.7 + Grain(screenUv * float2(38,26)) * 0.3;
                float3 normal = normalize(float3(-slope * 3.0, 1));
                float rim = pow(saturate(dot(normal, normalize(float3(-0.65,0.75,0.6)))), 18);
                float sheen = pow(saturate(1.0 - abs(pooling - 0.62) * 8.0), 5) * 0.13;
                float3 blood = input.color.rgb * (1.0 - _Thickness * (0.35 + pooling * 0.65));
                blood += float3(0.14,0.028,0.021) * (rim + sheen) * _Wetness;
                float4 result = float4(blood, alpha * input.color.a);
                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(input.localPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
