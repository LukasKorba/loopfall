Shader "Loopfall/Shield"
{
    Properties
    {
        _Color ("Shield Color", Color) = (0.2, 1.0, 0.4, 1)
        _Intensity ("Glow Intensity", Range(0, 5)) = 5.0
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 1.58
        _RimBoost ("Rim Boost", Range(0, 3)) = 0.83
        _NoiseScale ("Noise Scale", Range(0.5, 10)) = 2.37
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 0.58
        _PatchContrast ("Patch Contrast", Range(0.5, 6)) = 1.18
        _PatchStrength ("Patch Strength", Range(0, 2)) = 1.01
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.92
        _PulseDepth ("Pulse Depth", Range(0, 0.5)) = 0.167
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Blend SrcAlpha One   // Additive — never occludes the ball
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 objPos : TEXCOORD2;
            };

            fixed4 _Color;
            float _Intensity;
            float _FresnelPower;
            float _RimBoost;
            float _NoiseScale;
            float _NoiseSpeed;
            float _PatchContrast;
            float _PatchStrength;
            float _PulseSpeed;
            float _PulseDepth;

            float hash3(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float valueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = hash3(i + float3(0,0,0));
                float n100 = hash3(i + float3(1,0,0));
                float n010 = hash3(i + float3(0,1,0));
                float n110 = hash3(i + float3(1,1,0));
                float n001 = hash3(i + float3(0,0,1));
                float n101 = hash3(i + float3(1,0,1));
                float n011 = hash3(i + float3(0,1,1));
                float n111 = hash3(i + float3(1,1,1));
                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(UnityObjectToWorldNormal(v.normal));
                o.objPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float NdotV = saturate(dot(i.worldNormal, viewDir));

                // Fresnel — transparent at center, glowing at grazing angles.
                // Player sees their small ball clearly through the middle.
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _RimBoost;

                // Drifting 3D value noise, two octaves for more organic shapes
                float3 nPos = i.objPos * _NoiseScale + float3(_Time.y, _Time.y * 0.7, _Time.y * 0.5) * _NoiseSpeed;
                float n = valueNoise(nPos);
                n += 0.5 * valueNoise(nPos * 2.1 + 11.0);
                n /= 1.5;

                // Push noise into high-contrast patches — "islands"
                float patch = saturate(pow(abs(n - 0.5) * 2.0, _PatchContrast)) * _PatchStrength;

                // Global pulse — shield feels alive
                float pulse = 1.0 - _PulseDepth + _PulseDepth * sin(_Time.y * _PulseSpeed);

                // Fresnel carries the silhouette; patches add plasma over the whole sphere
                float alpha = saturate(fresnel + patch) * pulse;

                fixed4 col = _Color * _Intensity;
                col.rgb *= alpha;     // pre-multiply keeps additive blend clean
                col.a = alpha * _Color.a;
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
