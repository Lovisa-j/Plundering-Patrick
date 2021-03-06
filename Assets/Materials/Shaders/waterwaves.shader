Shader "Custom/waterwaves"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Glossiness", Range(0,10)) = 0.5
        /*_Metallic ("Metallic", Range(0,1)) = 0.0   */
        _SpecularColor("Specular", Color) = (0.2,0.2,0.2)
        _WaveA ("Wave A (dir, steepness, wavelength)", Vector) = (1,0,0.5,10)
        _WaveB ("Wave B", Vector) = (0,1,0.25,20)
        _WaveC ("Wave C", Vector) = (1,1,0.15,10)
        
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf StandardSpecular fullforwardshadows vertex:vert addshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        float _Glossiness;
        /*half _Metallic;*/
        fixed3 _SpecularColor;
        fixed4 _Color;        
        float4 _WaveA, _WaveB, _WaveC;

        

#if !defined(FLOW_INCLUDED)
#define FLOW_INCLUDED

        float2 FlowUV(float2 uv, float time) 
        {
            return uv + time;
        }
#endif

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            float2 uv = FlowUV(IN.uv_MainTex, _Time.y);
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            /*o.Metallic = _Metallic;*/
            o.Specular = _SpecularColor;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }

        float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal) 
        {
            float steepness = wave.z;
            float wavelength = wave.w;
            float k = 2 * UNITY_PI / wavelength;
            float c = sqrt(9.8 / k);
            float2 d = normalize(wave.xy);
            float f = k * (dot(d, p.xz) - c * _Time.y);
            float a = steepness / k;

            tangent += float3(-d.x * d.x * (steepness * sin(f)), d.x * (steepness * cos(f)), -d.x * d.y * (steepness * sin(f)));
            binormal += float3(-d.x * d.y * (steepness * sin(f)), d.y * (steepness * cos(f)), -d.y * d.y * (steepness * sin(f)));
            return float3(d.x * (a * cos(f)), a * sin(f), d.y * (a * cos(f)));
        }

        void vert(inout appdata_full vertexData) 
        {
            float3 gridPoint = vertexData.vertex.xyz;
            float3 tangent = float3(1, 0, 0);
            float3 binormal = float3(0, 0, 1);
            float3 p = gridPoint;
            p += GerstnerWave(_WaveA, gridPoint, tangent, binormal);
            p += GerstnerWave(_WaveB, gridPoint, tangent, binormal);
            p += GerstnerWave(_WaveC, gridPoint, tangent, binormal);
            float3 normal = normalize(cross(binormal, tangent));
            vertexData.vertex.xyz = p;
            vertexData.normal = normal;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
