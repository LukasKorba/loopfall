Shader "Loopfall/TrackGrid"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.12, 0.12, 0.14, 1)
        _GridColor1 ("Grid Color 1 (Major)", Color) = (0.0, 0.6, 0.8, 0.6)
        _GridColor2 ("Grid Color 2 (Minor)", Color) = (0.5, 0.1, 0.6, 0.3)
        _GridColor3 ("Grid Color 3 (Accent - U only)", Color) = (0.8, 0.4, 0.1, 0.2)
        _MajorLineWidth ("Major Line Width", Range(0.001, 0.05)) = 0.015
        _MinorLineWidth ("Minor Line Width", Range(0.001, 0.03)) = 0.006
        _MajorGridU ("Major Grid Lines (around tube)", Range(1, 64)) = 16
        _MajorGridV ("Major Grid Lines (along tube)", Range(1, 32)) = 8
        _MinorGridU ("Minor Grid Subdivisions U", Range(1, 8)) = 4
        _MinorGridV ("Minor Grid Subdivisions V", Range(1, 8)) = 2
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1.2
        _GlowFalloff ("Glow Falloff", Range(1, 20)) = 6
        _PulseSpeed ("Pulse Speed", Range(0, 2)) = 0.5
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.15
        _Glossiness ("Glossiness", Range(0, 1)) = 0.3
        _Metallic ("Metallic", Range(0, 1)) = 0.1
        _DepthFadeStart ("Depth Fade Start", Range(0, 10)) = 2.0
        _DepthFadeEnd ("Depth Fade End", Range(5, 50)) = 20.0
        _FarColor ("Far Grid Color", Color) = (0.2, 0.05, 0.3, 0.3)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float2 gridUV;
            float4 vertColor;
            float camDist;
        };

        fixed4 _BaseColor;
        fixed4 _GridColor1;
        fixed4 _GridColor2;
        fixed4 _GridColor3;
        float _MajorLineWidth;
        float _MinorLineWidth;
        float _MajorGridU;
        float _MajorGridV;
        float _MinorGridU;
        float _MinorGridV;
        float _GlowIntensity;
        float _GlowFalloff;
        float _PulseSpeed;
        float _PulseAmount;
        half _Glossiness;
        half _Metallic;
        float _DepthFadeStart;
        float _DepthFadeEnd;
        fixed4 _FarColor;

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.gridUV = v.texcoord;
            o.vertColor = v.color;
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.camDist = distance(worldPos, _WorldSpaceCameraPos);
        }

        float gridLine(float coord, float width, float falloff)
        {
            float d = abs(frac(coord + 0.5) - 0.5);
            float core = saturate(1.0 - d / width);
            float glow = exp(-d * falloff);
            return max(core, glow * 0.4);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.gridUV;
            float t = _Time.y;

            // Distance-based fade: 0 = near camera, 1 = far
            float depthT = saturate((IN.camDist - _DepthFadeStart) / (_DepthFadeEnd - _DepthFadeStart));

            // Major grid
            float majorU = gridLine(uv.x * _MajorGridU, _MajorLineWidth, _GlowFalloff);
            float majorV = gridLine(uv.y * _MajorGridV, _MajorLineWidth, _GlowFalloff);
            float major = max(majorU, majorV);

            // Minor grid
            float totalU = _MajorGridU * _MinorGridU;
            float totalV = _MajorGridV * _MinorGridV;
            float minorU = gridLine(uv.x * totalU, _MinorLineWidth, _GlowFalloff * 1.5);
            float minorV = gridLine(uv.y * totalV, _MinorLineWidth, _GlowFalloff * 1.5);
            float minor = max(minorU, minorV);

            // Pulse
            float pulse = 1.0 + _PulseAmount * sin(t * _PulseSpeed + uv.x * 6.28);

            // Near grid color (full palette)
            float3 nearCol = _GridColor1.rgb * major * _GridColor1.a;
            nearCol += _GridColor2.rgb * minor * _GridColor2.a * (1.0 - major * 0.5);

            // Accent: extra U-direction shimmer (no diagonals — those align with triangle edges)
            float accent = gridLine(uv.x * _MajorGridU * 1.5, _MinorLineWidth * 0.7, _GlowFalloff * 2.0);
            nearCol += _GridColor3.rgb * accent * _GridColor3.a * 0.3;

            // Far grid: single muted color, reduced intensity
            float totalGrid = saturate(major + minor * 0.5);
            float3 farCol = _FarColor.rgb * totalGrid * _FarColor.a;

            // Blend near → far based on distance
            float3 gridCol = lerp(nearCol, farCol, depthT);
            float intensity = lerp(_GlowIntensity, 0.0, depthT);

            // Base color with per-tile vertex color variation
            float3 base = _BaseColor.rgb * IN.vertColor.rgb;

            o.Albedo = base;
            o.Emission = gridCol * intensity * pulse * IN.vertColor.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness + totalGrid * 0.2;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    Fallback "Standard"
}
