// Procedurally generated night sky. No textures, no cubemaps.
// A gradient dome with faint stars, an overcast haze band and a cold moon.
Shader "WITCHBOUND/NightSky"
{
    Properties
    {
        _TopColor ("Zenith Color", Color) = (0.004, 0.007, 0.016, 1)
        _HorizonColor ("Horizon Color", Color) = (0.035, 0.045, 0.07, 1)
        _GroundColor ("Below Horizon Color", Color) = (0.008, 0.01, 0.016, 1)
        _MoonColor ("Moon Color", Color) = (0.72, 0.80, 0.96, 1)
        _MoonDir ("Moon Direction", Vector) = (0.42, 0.48, 0.77, 0)
        _MoonSize ("Moon Size", Range(0.005, 0.2)) = 0.038
        _StarIntensity ("Star Intensity", Range(0, 1)) = 0.22
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TopColor;
            fixed4 _HorizonColor;
            fixed4 _GroundColor;
            fixed4 _MoonColor;
            float4 _MoonDir;
            float _MoonSize;
            float _StarIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);

                // Vertical gradient: deep black-blue zenith down to a slightly
                // lighter smog band at the horizon.
                float h = saturate(d.y);
                fixed3 col = lerp(_HorizonColor.rgb, _TopColor.rgb, pow(h, 0.55));

                // Below the horizon the town fades into a near-black ground haze.
                col = lerp(_GroundColor.rgb, col, smoothstep(-0.10, 0.02, d.y));

                // A faint band of overcast light just above the horizon.
                float band = exp(-abs(d.y - 0.045) * 18.0);
                col += fixed3(0.020, 0.024, 0.034) * band;

                // Sparse, slightly varied stars (only above the haze).
                float3 sp = d * 260.0;
                float3 cell = floor(sp);
                float rnd = hash13(cell);
                float3 f = frac(sp) - 0.5;
                float starShape = saturate(1.0 - dot(f, f) * 9.0);
                float star = step(0.9972, rnd) * starShape;
                float twinkle = 0.55 + 0.45 * hash13(cell + 7.0);
                col += star * _StarIntensity * twinkle * saturate(d.y * 2.5);

                // Cold moon disc + soft halo.
                float3 moonDir = normalize(_MoonDir.xyz);
                float md = dot(d, moonDir);
                float disc = smoothstep(cos(_MoonSize), cos(_MoonSize * 0.86), md);
                float halo = pow(saturate(md), 700.0) * 0.22;
                float haze = pow(saturate(md), 90.0) * 0.045;
                col += _MoonColor.rgb * (disc * 0.95 + halo + haze);

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
