Shader "Game/UI/AlphaIcon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _InvertRGB ("Invert RGB", Range(0,1)) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct VertexInput { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct VertexOutput { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color;
            fixed _InvertRGB;
            float4 _ClipRect;
            VertexOutput Vert(VertexInput input)
            {
                VertexOutput output;
                output.world = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }
            fixed4 Frag(VertexOutput input) : SV_Target
            {
                fixed4 color = input.color;
                fixed4 textureColor = tex2D(_MainTex,input.uv);
                fixed3 sourceColor = textureColor.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                sourceColor = LinearToGammaSpace(sourceColor);
                #endif
                fixed3 invertedColor = 1-sourceColor;
                #ifndef UNITY_COLORSPACE_GAMMA
                invertedColor = GammaToLinearSpace(invertedColor);
                #endif
                color.rgb *= lerp(fixed3(1,1,1),invertedColor,_InvertRGB);
                color.a *= textureColor.a;
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.world.xy,_ClipRect);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
