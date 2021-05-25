Shader "1/Simple Diffuse"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
    
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Lambert exclude_path:prepass exclude_path:deferred noforwardadd noshadow nodynlightmap nolppv noshadowmask

        half4 _Color;

        UNITY_DECLARE_TEX2D(_MainTex);

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };
        
        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = UNITY_SAMPLE_TEX2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb * _Color * IN.color;
            o.Alpha = 1.0f;
        }
        ENDCG
    }

    FallBack "Diffuse"
}