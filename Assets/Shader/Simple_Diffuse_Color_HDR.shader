Shader "1/Simple Diffuse Color HDR"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
    
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Lambert exclude_path:prepass exclude_path:deferred noforwardadd noshadow nodynlightmap nolppv noshadowmask

        half4 _Color;

        struct Input
        {
            float4 color : COLOR;
        };
        
        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Color * IN.color;
            o.Alpha = 1.0f;
        }
        ENDCG
    }

    FallBack "Diffuse"
}