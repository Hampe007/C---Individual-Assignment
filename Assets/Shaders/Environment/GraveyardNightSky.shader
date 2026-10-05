Shader "Game/Atmosphere/GraveyardNightSky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.025,0.033,0.064,1)
        _Horizon ("Horizon", Color) = (0.048,0.032,0.052,1)
        _CloudColor ("Moonlit Clouds", Color) = (0.12,0.09,0.14,1)
        _Stars ("Star Brightness", Range(0,2)) = 0.8
        [Toggle] _MoonEnabled ("Celestial Moon Enabled", Float) = 1
        _MoonDirection ("Moon World Direction", Vector) = (-0.251,0.126,0.960,0)
        _MoonDiameter ("Moon Angular Diameter (Degrees)", Range(0.1,10)) = 2.05
        _MoonDisc ("Lunar Disc (RGBA)", 2D) = "white" {}
        _MoonTint ("Lunar Tint", Color) = (0.28,0.012,0.022,1)
        [HDR] _MoonRadiance ("Moon Radiance", Color) = (2.7,0.09,0.06,1)
        _MoonPulsePeriod ("Moon Pulse Period (Seconds)", Range(1,30)) = 9
        _MoonPulseStrength ("Moon Pulse Strength", Range(0,1)) = 0.2
        _HaloDiameter ("Halo Angular Diameter (Degrees)", Range(0.1,90)) = 6.2
        [HDR] _HaloInner ("Halo Inner Blood Red", Color) = (0.09,0.002,0.004,1)
        [HDR] _HaloOuter ("Halo Outer Soft Red", Color) = (0.18,0.025,0.03,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _Zenith, _Horizon, _CloudColor;
            float _Stars;
            TEXTURE2D(_MoonDisc); SAMPLER(sampler_MoonDisc);
            float4 _MoonDirection, _MoonTint, _MoonRadiance, _HaloInner, _HaloOuter;
            float _MoonEnabled, _MoonDiameter, _MoonPulsePeriod, _MoonPulseStrength, _HaloDiameter;
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            float Hash(float3 samplePosition) { return frac(sin(dot(samplePosition, float3(127.1,311.7,74.7))) * 43758.5453); }
            float Noise(float3 samplePosition)
            {
                float3 cell = floor(samplePosition), blend = frac(samplePosition);
                blend = blend * blend * (3 - 2 * blend);
                float bottom = lerp(lerp(Hash(cell), Hash(cell+float3(1,0,0)), blend.x), lerp(Hash(cell+float3(0,1,0)), Hash(cell+float3(1,1,0)), blend.x), blend.y);
                float top = lerp(lerp(Hash(cell+float3(0,0,1)), Hash(cell+float3(1,0,1)), blend.x), lerp(Hash(cell+float3(0,1,1)), Hash(cell+1), blend.x), blend.y);
                return lerp(bottom, top, blend.z);
            }
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 viewDirection = mul((float3x3)UNITY_MATRIX_V,input.positionOS.xyz);
                output.positionCS = mul(UNITY_MATRIX_P,float4(viewDirection,1));
                output.positionCS.z = UNITY_RAW_FAR_CLIP_VALUE*output.positionCS.w;
                output.direction = input.positionOS.xyz;
                return output;
            }
            float4 MoonSurface(float2 uv)
            {
                float4 lunarDisc = SAMPLE_TEXTURE2D(_MoonDisc,sampler_MoonDisc,uv);
                float brightness = pow(saturate(dot(lunarDisc.rgb,float3(0.3,0.59,0.11))),1.6)*0.2;
                float pulse = 1+_MoonPulseStrength*(0.5-0.5*cos(_Time.y*TWO_PI/max(_MoonPulsePeriod,0.01)));
                return float4(_MoonRadiance.rgb*pulse*brightness+_MoonTint.rgb*lunarDisc.rgb*0.15,lunarDisc.a);
            }
            half4 Fragment(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float altitude = saturate(direction.y);
                float3 sky = lerp(_Horizon.rgb, _Zenith.rgb, smoothstep(0,0.55,altitude));
                float3 moonAxis = normalize(_MoonDirection.xyz+float3(0,0,0.000001));
                float moonFacing = dot(direction,moonAxis);
                float moonAngle = radians(_MoonDiameter*0.5);
                float haloAngle = max(radians(_HaloDiameter*0.5),moonAngle+0.0001);
                float haloRadius = saturate((acos(clamp(moonFacing,-1,1))-moonAngle)/(haloAngle-moonAngle));
                float haloFade = 1-haloRadius;
                float haloGlow = haloFade*haloFade*haloFade;
                sky += lerp(_HaloInner.rgb,_HaloOuter.rgb,smoothstep(0,1,haloRadius))*haloGlow*saturate(_MoonEnabled);
                float moonRadius = sin(moonAngle);
                float distanceSquared = max(0,1-moonFacing*moonFacing);
                float edgeWidth = max(fwidth(distanceSquared),0.0000001);
                float coverage = saturate((moonRadius*moonRadius-distanceSquared)/edgeWidth+0.5)*step(0,moonFacing)*saturate(_MoonEnabled);
                if (coverage > 0)
                {
                    float3 referenceUp = abs(moonAxis.y) > 0.99 ? float3(0,0,1) : float3(0,1,0);
                    float3 moonRight = normalize(cross(referenceUp,moonAxis));
                    float3 moonUp = cross(moonAxis,moonRight);
                    float2 uv = float2(dot(direction,moonRight),dot(direction,moonUp))/max(moonRadius,0.000001)*0.5+0.5;
                    float4 lunarDisc = MoonSurface(uv);
                    coverage *= lunarDisc.a;
                    sky = lerp(sky,lunarDisc.rgb,coverage);
                }
                float3 drift = direction * float3(5,12,5) + float3(_Time.y*0.0015,0,_Time.y*0.0008);
                float cloudNoise = Noise(drift)*0.57 + Noise(drift*2.1)*0.28 + Noise(drift*4.3)*0.15;
                float clouds = smoothstep(0.43,0.7,cloudNoise) * smoothstep(0,0.16,altitude);
                sky = lerp(sky,_CloudColor.rgb,clouds*0.65);
                float3 starGrid = direction * 420;
                float star = step(0.995,Hash(floor(starGrid))) * pow(saturate(1-length(frac(starGrid)-0.5)*2),4);
                sky += star * _Stars * (1-clouds) * smoothstep(0.03,0.25,altitude) * (1-coverage);
                return half4(sky,1);
            }
            ENDHLSL
        }
    }
}
