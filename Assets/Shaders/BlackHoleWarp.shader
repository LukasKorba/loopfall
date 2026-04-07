Shader "Loopfall/BlackHoleWarp"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
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

            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 center = float2(_FocalPointX, _FocalPointY);
                float2 delta = uv - center;

                // Aspect-corrected distance — circular influence regardless of screen shape
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float cx = delta.x * aspect;
                float dist = sqrt(cx * cx + delta.y * delta.y);

                // Raw UV-space direction (for applying offset in UV coords)
                float rawDist = length(delta);
                float2 dir = (rawDist > 0.001) ? (delta / rawDist) : float2(0, 0);

                // Gravity well: pull strongest at center, fading to zero at radius
                float t = saturate(dist / _WarpRadius);
                float influence = pow(1.0 - t, _WarpFalloff);

                // Push sample point AWAY from center
                // → pixels show content from further out → visual convergence toward center
                float2 warpedUV = uv + dir * influence * _WarpStrength;
                warpedUV = clamp(warpedUV, float2(0.001, 0.001), float2(0.999, 0.999));

                return tex2D(_MainTex, warpedUV);
            }
            ENDCG
        }
    }
    Fallback Off
}
