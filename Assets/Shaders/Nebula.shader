Shader "Loopfall/Nebula"
{
    Properties
    {
        _Speed ("Animation Speed", Range(0, 0.5)) = 0.08
        _Scale ("Noise Scale", Range(0.5, 5)) = 1.5
        _Brightness ("Brightness", Range(0, 1)) = 0.35
        _Color1 ("Color 1 (Deep)", Color) = (0.06, 0.02, 0.15, 1)
        _Color2 ("Color 2 (Mid)", Color) = (0.02, 0.1, 0.2, 1)
        _Color3 ("Color 3 (Highlight)", Color) = (0.1, 0.02, 0.08, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "IgnoreProjector"="True" }
        LOD 100
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            float _Speed;
            float _Scale;
            float _Brightness;
            fixed4 _Color1;
            fixed4 _Color2;
            fixed4 _Color3;

            // Simplex-like hash noise
            float2 hash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453);
            }

            float simplex(float2 p)
            {
                const float K1 = 0.366025404; // (sqrt(3)-1)/2
                const float K2 = 0.211324865; // (3-sqrt(3))/6

                float2 i = floor(p + (p.x + p.y) * K1);
                float2 a = p - i + (i.x + i.y) * K2;
                float2 o = (a.x > a.y) ? float2(1, 0) : float2(0, 1);
                float2 b = a - o + K2;
                float2 c = a - 1.0 + 2.0 * K2;

                float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
                h = h * h * h * h;

                float3 n = h * float3(
                    dot(a, hash(i)),
                    dot(b, hash(i + o)),
                    dot(c, hash(i + 1.0))
                );

                return dot(n, float3(70.0, 70.0, 70.0));
            }

            // Fractal brownian motion — layered noise
            float fbm(float2 p)
            {
                float f = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 5; i++)
                {
                    f += amp * simplex(p);
                    p *= 2.1;
                    amp *= 0.5;
                }
                return f;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 uv = i.uv * _Scale;

                // Two noise layers with different drift directions
                float n1 = fbm(uv + float2(t * 0.7, t * 0.3));
                float n2 = fbm(uv * 1.3 + float2(-t * 0.4, t * 0.6) + 5.0);

                // Warp: feed noise back into itself for organic swirls
                float n3 = fbm(uv + float2(n1, n2) * 0.5 + t * 0.2);

                // Map to color palette
                float mix1 = n3 * 0.5 + 0.5; // 0..1
                float mix2 = n1 * 0.5 + 0.5;

                fixed4 col = lerp(_Color1, _Color2, mix1);
                col = lerp(col, _Color3, mix2 * 0.4);
                col *= _Brightness;
                col.a = 1.0;

                return col;
            }
            ENDCG
        }
    }
}
