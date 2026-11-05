Shader "ReTune/WindingKeyMetal"
{
    Properties
    {
        _BaseColor("Metal Color", Color) = (1,1,1,1)
        _BaseMap("Original hand-painted key", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.uv=input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal=normalize(input.normalWS);
                float3 light=normalize(float3(-.4,.6,-1));
                half diffuse=saturate(dot(normal,light));
                half glint=pow(saturate(dot(normal,normalize(light+float3(0,0,-1)))),24);
                half4 paint=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
                return half4(paint.rgb*_BaseColor.rgb*(.65+.4*diffuse)+half3(.035,.03,.025)*glint,1);
            }
            ENDHLSL
        }
    }
}
