Shader "Loopfall/Ball"
{
    Properties
    {
        _Color ("Base Color", Color) = (0.9, 0.92, 1.0, 1)
        _RimColor ("Rim Color", Color) = (0.0, 0.75, 1.0, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _RimStrength ("Rim Strength", Range(0, 3)) = 1.2
        _Glossiness ("Glossiness", Range(0, 1)) = 0.85
        _Metallic ("Metallic", Range(0, 1)) = 0.3
        _EmissionBase ("Base Emission", Color) = (0.15, 0.15, 0.2, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float3 viewDir;
        };

        fixed4 _Color;
        fixed4 _RimColor;
        half _RimPower;
        half _RimStrength;
        half _Glossiness;
        half _Metallic;
        fixed4 _EmissionBase;

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            // Fresnel rim glow — neon edge lighting
            float rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            float rimFactor = pow(rim, _RimPower) * _RimStrength;
            o.Emission = _EmissionBase.rgb + _RimColor.rgb * rimFactor;

            o.Alpha = 1.0;
        }
        ENDCG
    }
    Fallback "Mobile/Diffuse"
}
