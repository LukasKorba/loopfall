Shader "Loopfall/PostProcess"
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
        _FocalPointX ("Focal Point X", Float) = 0.5
        _FocalPointY ("Focal Point Y", Float) = 0.88
        _WarpStrength ("Warp Strength", Float) = 0.15
        _WarpRadius ("Warp Radius", Float) = 0.5
        _WarpFalloff ("Warp Falloff", Float) = 1.5
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
            float _FocalPointX;
            float _FocalPointY;
            float _WarpStrength;
            float _WarpRadius;
            float _WarpFalloff;

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
                // ── BLACK HOLE WARP (was first pass) ─────────────────────
                // Aspect-corrected distance from focal point gives a circular
                // influence regardless of screen shape; pull sample point AWAY
                // from center so pixels show content from further out → visual
                // convergence toward the focal point.
                float2 center = float2(_FocalPointX, _FocalPointY);
                float2 delta = i.uv - center;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float cx = delta.x * aspect;
                float dist = sqrt(cx * cx + delta.y * delta.y);
                float rawDist = length(delta);
                float2 dir = (rawDist > 0.001) ? (delta / rawDist) : float2(0, 0);
                float tWarp = saturate(dist / _WarpRadius);
                float influence = pow(1.0 - tWarp, _WarpFalloff);
                float2 warpedUV = i.uv + dir * influence * _WarpStrength;
                warpedUV = clamp(warpedUV, float2(0.001, 0.001), float2(0.999, 0.999));

                half4 col = tex2D(_MainTex, warpedUV);

                // ── DEPTH HUE SHIFT + FOG (was second pass) ──────────────
                // Depth is sampled at the ORIGINAL screen UV, not warped —
                // depth buffer is pre-warp and the hue/fog gradient keys off
                // where the pixel is on screen, not what's drawn into it.
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float eyeDepth = LinearEyeDepth(rawDepth);

                if (eyeDepth > 500.0)
                    return col;

                float t = saturate((eyeDepth - _DepthStartWorld) / (_DepthEndWorld - _DepthStartWorld));

                // HDR pickups (orbs/beams at intensity 2+) keep their identity color.
                // Threshold 1.0 lets regular emissive obstacles (Pure Hell gates, lum ~0.85)
                // hue-shift with depth for the rainbow tunnel look; only pixels pushed past
                // unity-RGB by high-intensity additive blending stay stable.
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float brightReduce = saturate((lum - 1.0) * 4.0);
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
