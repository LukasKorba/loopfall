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
        _SparkIntensity ("Spark Intensity", Range(0, 3)) = 1.5
        _SparkSize ("Spark Size", Range(0.005, 0.1)) = 0.025
        _SparkColor1 ("Spark Color 1", Color) = (0.2, 0.8, 1.0, 1)
        _SparkColor2 ("Spark Color 2", Color) = (1.0, 0.4, 0.8, 1)
        _SparkColor3 ("Spark Color 3", Color) = (0.3, 1.0, 0.5, 1)
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
            float3 worldPos;
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
        float _SparkIntensity;
        float _SparkSize;
        fixed4 _SparkColor1;
        fixed4 _SparkColor2;
        fixed4 _SparkColor3;

        // Score pulse — set via Shader.SetGlobal from C#
        float _ScorePulseTime;
        float4 _ScorePulsePos;

        // Death pulse — dual shockwave rings
        float _DeathPulseTime;
        float4 _DeathPulsePos;

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.gridUV = v.texcoord;
            o.vertColor = v.color;
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.camDist = distance(wp, _WorldSpaceCameraPos);
            o.worldPos = wp;
        }

        float gridLine(float coord, float width, float falloff)
        {
            float d = abs(frac(coord + 0.5) - 0.5);
            float core = saturate(1.0 - d / width);
            float glow = exp(-d * falloff);
            return max(core, glow * 0.4);
        }

        // Single spark traveling along a grid line
        // lineCoord: position along the line (U or V)
        // crossCoord: distance from the grid line (snapped to nearest)
        // speed: travel speed
        // offset: phase offset so sparks don't overlap
        float spark(float lineCoord, float crossCoord, float gridCount,
                     float speed, float offset, float size)
        {
            // Snap to nearest grid line — spark only appears ON a line
            float nearest = floor(crossCoord * gridCount + 0.5) / gridCount;
            float lineDist = abs(crossCoord - nearest);
            float onLine = exp(-lineDist * gridCount * 12.0); // Sharp falloff from line

            // Traveling position along the line
            float pos = frac(lineCoord + _Time.y * speed + offset);
            float dist = abs(pos - 0.5) * 2.0; // Distance from spark center (0..1)

            // Sharp gaussian peak for the spark
            float sparkGlow = exp(-dist * dist / (size * size));

            // Small bright tail trailing behind
            float tailDir = (speed > 0) ? 1.0 : -1.0;
            float tail = exp(-(pos - 0.5 + tailDir * 0.03) * (pos - 0.5 + tailDir * 0.03) / (size * size * 6.0));

            return (sparkGlow + tail * 0.3) * onLine;
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

            // ── TRAVELING SPARKS ──────────────────────────────────
            float3 sparkTotal = float3(0, 0, 0);

            // U-direction sparks (traveling around the tube, along V grid lines)
            sparkTotal += _SparkColor1.rgb * spark(uv.x, uv.y, _MajorGridV, 0.12, 0.0, _SparkSize);
            sparkTotal += _SparkColor2.rgb * spark(uv.x, uv.y, _MajorGridV, -0.08, 0.37, _SparkSize);
            sparkTotal += _SparkColor1.rgb * spark(uv.x, uv.y, _MajorGridV, 0.15, 0.72, _SparkSize * 0.7) * 0.6;

            // V-direction sparks (traveling along the tube, along U grid lines)
            sparkTotal += _SparkColor3.rgb * spark(uv.y, uv.x, _MajorGridU, 0.10, 0.15, _SparkSize);
            sparkTotal += _SparkColor2.rgb * spark(uv.y, uv.x, _MajorGridU, -0.06, 0.55, _SparkSize);
            sparkTotal += _SparkColor3.rgb * spark(uv.y, uv.x, _MajorGridU, 0.18, 0.88, _SparkSize * 0.7) * 0.6;

            // Fade sparks with distance (less visible far away)
            float sparkFade = 1.0 - depthT * 0.8;
            nearCol += sparkTotal * _SparkIntensity * sparkFade;

            // Far grid: single muted color, reduced intensity
            float totalGrid = saturate(major + minor * 0.5);
            float3 farCol = _FarColor.rgb * totalGrid * _FarColor.a;

            // Blend near → far based on distance
            float3 gridCol = lerp(nearCol, farCol, depthT);
            float intensity = lerp(_GlowIntensity, 0.0, depthT);

            // ── SCORE PULSE WAVE ──────────────────────────────────
            float timeSincePulse = _Time.y - _ScorePulseTime;
            if (timeSincePulse > 0.0 && timeSincePulse < 1.0)
            {
                float dist = distance(IN.worldPos, _ScorePulsePos.xyz);
                float ringRadius = timeSincePulse * 8.0; // Expand at 8 units/sec
                float ringDist = abs(dist - ringRadius);
                float ringGlow = exp(-ringDist * ringDist * 40.0); // Sharp ring
                float attack = smoothstep(0.0, 0.08, timeSincePulse); // Quick ramp-in
                float fade = attack * (1.0 - timeSincePulse); // Ramp in then fade out
                // Only glow on grid lines — multiply by existing grid presence
                float onGrid = saturate(totalGrid * 3.0);
                float3 pulseColor = lerp(_GridColor1.rgb, float3(1, 1, 1), 0.3);
                gridCol += pulseColor * ringGlow * fade * fade * onGrid * 1.6;
            }

            // ── DEATH PULSE WAVES ─────────────────────────────────
            float timeSinceDeath = _Time.y - _DeathPulseTime;
            if (timeSinceDeath > 0.0 && timeSinceDeath < 1.5)
            {
                float dDist = distance(IN.worldPos, _DeathPulsePos.xyz);
                float onGridD = saturate(totalGrid * 3.0);

                // Ring 1: fast outward blast
                float r1Radius = timeSinceDeath * 12.0;
                float r1Dist = abs(dDist - r1Radius);
                float r1Glow = exp(-r1Dist * r1Dist * 30.0);
                float r1Fade = smoothstep(0.0, 0.05, timeSinceDeath)
                             * (1.0 - saturate(timeSinceDeath / 1.0));

                // Ring 2: slower, delayed, wider
                float r2Time = timeSinceDeath - 0.12;
                float r2Radius = max(0.0, r2Time) * 6.0;
                float r2Dist = abs(dDist - r2Radius);
                float r2Glow = exp(-r2Dist * r2Dist * 20.0);
                float r2Fade = smoothstep(0.0, 0.1, r2Time)
                             * (1.0 - saturate(r2Time / 1.2));

                float3 deathColor1 = float3(1.0, 0.15, 0.4);  // Hot magenta
                float3 deathColor2 = float3(0.8, 0.05, 0.2);  // Deep red

                gridCol += deathColor1 * r1Glow * r1Fade * r1Fade * onGridD * 2.0;
                gridCol += deathColor2 * r2Glow * r2Fade * r2Fade * onGridD * 1.5;
            }

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
