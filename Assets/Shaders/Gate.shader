Shader "Loopfall/Gate"
{
    Properties
    {
        _Color ("Base Color", Color) = (1, 0.6, 0.15, 1)
        _EmissionColor ("Emission Color", Color) = (1.2, 0.6, 0.12, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 3)) = 1.0
        _DiffuseStrength ("Diffuse Strength", Range(0, 1)) = 0.35
        _AmbientBoost ("Ambient Boost", Range(0, 1)) = 0.15
        _SpawnProgress ("Spawn Progress", Range(0, 1)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                SHADOW_COORDS(2)
            };

            half4 _Color;
            half4 _EmissionColor;
            float _EmissionIntensity;
            float _DiffuseStrength;
            float _AmbientBoost;
            float _SpawnProgress;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                // Half-lambert diffuse — soft wrap lighting, no harsh dark side
                float NdotL = dot(normal, lightDir);
                float diffuse = NdotL * 0.5 + 0.5;
                diffuse = diffuse * diffuse; // Slightly sharper falloff

                // Shadow
                float shadow = SHADOW_ATTENUATION(i);

                // Rim light — subtle edge glow facing away from camera
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = pow(rim, 3.0) * 0.3;

                // Combine: diffuse lit color + constant emission + rim
                float3 litColor = _Color.rgb * diffuse * _DiffuseStrength * shadow;
                float3 ambient = _Color.rgb * _AmbientBoost;
                float3 emission = _EmissionColor.rgb * _EmissionIntensity;

                float3 normalResult = litColor + ambient + emission + rim * _EmissionColor.rgb;

                // Spawn effect: sp=0 invisible, sp→0.3 white-hot glow fading in, sp=1 normal
                float sp = saturate(_SpawnProgress);
                float3 spawnGlow = _EmissionColor.rgb * 4.0;
                float3 result = lerp(spawnGlow, normalResult, smoothstep(0.15, 0.8, sp));
                float alpha = smoothstep(0.0, 0.15, sp);

                return half4(result, alpha);
            }
            ENDCG
        }
    }
    Fallback "Mobile/Diffuse"
}
