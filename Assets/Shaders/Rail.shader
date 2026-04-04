Shader "Loopfall/Rail"
{
    Properties
    {
        _NearColor ("Near Color", Color) = (0.9, 0.1, 0.5, 1)
        _FarColor ("Far Color", Color) = (0.2, 0.05, 0.3, 1)
        _NearEmission ("Near Emission", Color) = (0.5, 0.03, 0.3, 1)
        _FarEmission ("Far Emission", Color) = (0.05, 0.02, 0.1, 1)
        _Glossiness ("Glossiness", Range(0, 1)) = 0.7
        _Metallic ("Metallic", Range(0, 1)) = 0.3
        _DepthFadeStart ("Depth Fade Start", Range(0, 10)) = 2.0
        _DepthFadeEnd ("Depth Fade End", Range(5, 50)) = 18.0
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
            float camDist;
        };

        fixed4 _NearColor;
        fixed4 _FarColor;
        fixed4 _NearEmission;
        fixed4 _FarEmission;
        half _Glossiness;
        half _Metallic;
        float _DepthFadeStart;
        float _DepthFadeEnd;

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.camDist = distance(worldPos, _WorldSpaceCameraPos);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float depthT = saturate((IN.camDist - _DepthFadeStart) / (_DepthFadeEnd - _DepthFadeStart));

            o.Albedo = lerp(_NearColor.rgb, _FarColor.rgb, depthT);
            o.Emission = lerp(_NearEmission.rgb, _FarEmission.rgb, depthT);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    Fallback "Standard"
}
