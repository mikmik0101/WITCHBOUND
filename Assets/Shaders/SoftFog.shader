// Unlit radial-falloff transparent shader used for ground-fog sheets and
// light dust particles. The alpha falls off toward the quad edges, so no
// textures need to be imported or serialized.
Shader "WITCHBOUND/SoftFog"
{
    Properties
    {
        _Color ("Color", Color) = (0.45, 0.50, 0.62, 0.10)
        _MainTex ("Texture", 2D) = "white" {}
        _Falloff ("Edge Falloff", Range(0.4, 8)) = 1.8
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Falloff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float r = saturate(1.0 - dot(p, p));
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 c = _Color * tex;
                c.a *= pow(r, _Falloff);
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
