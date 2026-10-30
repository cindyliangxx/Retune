Shader "ReTune/Demo Ribbon Glow"
{
    Properties
    {
        _Strength ("Strength", Float) = 1
        _Flash ("Judgement flash", Float) = 0
        [HDR] _LineColor ("Line HDR color", Color) = (.3,.65,1,1)
        _Emission ("Light intensity", Range(1,8)) = 2.4
        _CoreWidth ("Solid core", Range(.1,.8)) = .14
        _GlowStrength ("Soft halo", Range(0,1)) = .12
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 screen:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            float _Strength, _Flash, _Emission, _CoreWidth, _GlowStrength;
            float4 _LineColor;
            CBUFFER_END
            Varyings vert(Attributes i)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                o.screen=ComputeScreenPos(o.positionCS); o.color=i.color; o.uv=i.uv; return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                float x=i.screen.x/i.screen.w;
                float edge=abs(i.uv.y*2-1);
                float core=exp(-pow(edge/_CoreWidth,2));
                float halo=exp(-edge*edge*5)*_GlowStrength;
                float3 tint=_LineColor.rgb;
                float flare=_Flash*exp(-pow((x-.55)*16,2));
                float fade=smoothstep(0,.06,x)*(1-smoothstep(.94,1,x));
                // Keep HDR energy when whitening the narrow core; preserve each track's hue.
                tint=lerp(tint,max(tint,float3(1,1,1)),core*.22);
                return half4(tint*(_Emission+flare*5), saturate(core+halo)*fade*i.color.a*_LineColor.a*_Strength);
            }
            ENDHLSL
        }
    }
}
