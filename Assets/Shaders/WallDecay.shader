// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/WallDecay"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Occlusion ("Occlusion Map", 2D) = "white" {}
        _SpecMap ("Specular map", 2D) = "spec" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}

        _MossTex ("Moss Texture", 2D) = "moss" {}
        _MinCoords ("Minimum Bounding Box Coordinates", Vector) = (0, 0, 0)
        _MaxCoords ("Maximum Bounding Box Coordinates", Vector) = (0, 0, 0)
        _TimeScale ("Time Scale", float) = 0.005
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf StandardSpecular fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float2 uv_GlossMap;
            float2 uv_SpecMap;
            float3 worldPos;
        };

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _SpecMap;
        sampler2D _Occlusion;

        sampler2D _MossTex;
        float3 _MinCoords;
        float3 _MaxCoords;
        float _TimeScale;

        fixed4 _Color;
        half _Shininess;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        }

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 texMoss = tex2D(_MossTex, IN.uv_MainTex);
            fixed4 Occ = tex2D(_Occlusion, IN.uv_MainTex);
            fixed4 specTex = tex2D(_SpecMap, IN.uv_SpecMap);
            fixed4 mossTex = tex2D(_MossTex, IN.uv_MainTex);

            float time = _Time.y * _TimeScale + 0.01;
            float minY = _MinCoords.y;
            float maxY = _MaxCoords.y;
            float y = clamp(IN.worldPos.y, minY, maxY);

            float yNorm = (y - minY) / (maxY - minY);
            float fact = clamp(time * (1.0 - yNorm), 0.0, 1.0);
            tex = lerp(tex, mossTex, fact);

            o.Albedo = tex.rgb * _Color.rgb * Occ.a;
            o.Alpha = _Color.a;
            o.Specular = _Shininess * specTex.g;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
        }
        ENDCG
    }
    FallBack "Diffuse"
}
