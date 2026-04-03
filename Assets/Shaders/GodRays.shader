Shader "Loopfall/GodRays"
{
    Properties
    {
        _RayCount ("Ray Count", Range(3, 20)) = 8
        _RaySharpness ("Ray Sharpness", Range(1, 20)) = 6
        _RayLength ("Ray Length", Range(0.1, 1)) = 0.7
        _Intensity ("Intensity", Range(0, 2)) = 0.5
        _RotationSpeed ("Rotation Speed", Range(-0.5, 0.5)) = 0.03
        _Color1 ("Ray Color 1", Color) = (0.15, 0.05, 0.3, 1)
        _Color2 ("Ray Color 2", Color) = (0.05, 0.15, 0.25, 1)
        _CenterX ("Center X", Range(0, 1)) = 0.5
        _CenterY ("Center Y", Range(0, 1)) = 0.45
        _PulseSpeed ("Pulse Speed", Range(0, 2)) = 0.4
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.15
    }
    SubShader
    {
        Tags { "Queue"="Transparent-100" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Blend SrcAlpha One  // Additive — rays add light on top of nebula
            ZWrite Off
            Cull Off

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

            float _RayCount;
            float _RaySharpness;
            float _RayLength;
            float _Intensity;
            float _RotationSpeed;
            fixed4 _Color1;
            fixed4 _Color2;
            float _CenterX;
            float _CenterY;
            float _PulseSpeed;
            float _PulseAmount;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = float2(_CenterX, _CenterY);
                float2 uv = i.uv - center;

                // Correct aspect ratio so rays are circular
                float aspect = _ScreenParams.x / _ScreenParams.y;
                uv.x *= aspect;

                float dist = length(uv);
                float angle = atan2(uv.y, uv.x);

                // Slowly rotate rays
                float t = _Time.y;
                angle += t * _RotationSpeed;

                // Pulse
                float pulse = 1.0 + _PulseAmount * sin(t * _PulseSpeed);

                // Radial rays using sin pattern
                float rays = pow(abs(sin(angle * _RayCount)), _RaySharpness);

                // Fade with distance from center — bright near center, fade at edges
                float radialFade = saturate(1.0 - dist / (_RayLength * pulse));
                radialFade = radialFade * radialFade; // Squared falloff for softer edges

                // Color varies by angle
                float colorMix = sin(angle * 2.0 + t * 0.3) * 0.5 + 0.5;
                fixed4 col = lerp(_Color1, _Color2, colorMix);

                float alpha = rays * radialFade * _Intensity;

                return fixed4(col.rgb, alpha);
            }
            ENDCG
        }
    }
}
