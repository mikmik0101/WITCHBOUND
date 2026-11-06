// Unlit, alpha-blended shader for blood stains on the ground.
//
// The built-in transparent shaders are not reliable here: Unlit/Transparent is
// stripped out of the player build, and Sprites/Default multiplies by the mesh
// vertex colour, which the built-in quad does not have - so stains rendered as
// nothing at all. This shader samples the splatter texture directly, so the
// baked alpha of the generated texture is what the player actually sees.
Shader "WITCHBOUND/BloodDecal"
{
    Properties
    {
        _MainTex ("Splatter", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _AlphaMul ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Lighting Off
        // Lift the stain a hair toward the camera so it never fights the road
        // surface for depth even when both sit on the same plane.
        Offset -1, -1

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _AlphaMul;

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
                // Deliberately no vertex-colour term. The built-in quad has no
                // COLOR stream, and reading one here gives (0,0,0,0) on this
                // build - which is precisely why Sprites/Default drew nothing.
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 c = tex * _Color;
                c.a *= _AlphaMul;
                clip(c.a - 0.004);
                return c;
            }
            ENDCG
        }
    }

    Fallback Off
}
