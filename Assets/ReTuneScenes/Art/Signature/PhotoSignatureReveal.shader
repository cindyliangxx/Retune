Shader "ReTune/PhotoSignatureReveal"
{
    Properties
    {
        [PerRendererData] _MainTex("Signature reference",2D)="white"{}
        _StrokeMask("Stroke reveal",2D)="black"{}
        _Color("Ink",Color)=(1,1,1,1)
        _MaskRect("Mask crop",Vector)=(.265104,.223148,.376042,.476852)
        _FullReveal("Complete",Float)=0
        _Visible("Visible",Float)=0
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}
        Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 world:TEXCOORD1;};
            sampler2D _MainTex,_StrokeMask;
            fixed4 _Color;float4 _ClipRect,_MaskRect;float _FullReveal,_Visible;
            v2f vert(appdata v){v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 sample=tex2D(_MainTex,i.uv);
                float light=min(sample.r,min(sample.g,sample.b));
                float ink=smoothstep(.87,.98,light)*sample.a;
                float mask=tex2D(_StrokeMask,_MaskRect.xy+i.uv*_MaskRect.zw).a;
                fixed4 c=fixed4(i.color.rgb,ink*max(mask,_FullReveal)*_Visible*i.color.a);
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
