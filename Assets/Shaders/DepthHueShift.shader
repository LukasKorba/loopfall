Shader "Loopfall/DepthHueShift"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
        _HueShiftAmount ("Hue Shift Amount", Range(0, 1)) = 0.4
        _DepthStartWorld ("Near Distance (units)", Float) = 1.0
        _DepthEndWorld ("Far Distance (units)", Float) = 12.0
        _Saturation ("Saturation Boost", Range(0, 0.5)) = 0.15
        _FogColor ("Fog Color", Color) = (0.05, 0.03, 0.1, 1)
        _FogAmount ("Fog Amount", Range(0, 1)) = 0.6
        _FogStart ("Fog Start (units)", Float) = 3.0
        _FogEnd ("Fog End (units)", Float) = 18.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always ZWrite Off Cull Off

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

            sampler2D _MainTex;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float _HueShiftAmount;
            float _DepthStartWorld;
            float _DepthEndWorld;
            float _Saturation;
            half4 _FogColor;
            float _FogAmount;
            float _FogStart;
            float _FogEnd;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);

                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float eyeDepth = LinearEyeDepth(rawDepth);

                if (eyeDepth > 500.0)
                    return col;

                float t = saturate((eyeDepth - _DepthStartWorld) / (_DepthEndWorld - _DepthStartWorld));

                // Bright emissive pixels (orbs, beams, gates) keep their true color.
                // Threshold lowered to let high-intensity pickups (orbs) stay color-stable
                // at all depths — identity > atmospheric effect.
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float brightReduce = saturate((lum - 0.5) * 4.0);
                t *= (1.0 - brightReduce);

                float3 hsv = rgb2hsv(col.rgb);
                hsv.x = frac(hsv.x + t * _HueShiftAmount);
                hsv.y = saturate(hsv.y + t * _Saturation);
                float3 shifted = hsv2rgb(hsv);
                float3 result = lerp(col.rgb, shifted, t);

                float fogT = saturate((eyeDepth - _FogStart) / (_FogEnd - _FogStart));
                fogT = fogT * fogT;
                fogT *= (1.0 - brightReduce); // bright emissive pixels resist fog too
                result = lerp(result, _FogColor.rgb, fogT * _FogAmount);

                return half4(result, col.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
