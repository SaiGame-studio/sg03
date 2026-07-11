Shader "Custom/CardURPOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 0, 1)
        _OutlineThickness ("Outline Thickness", Range(0.0, 0.5)) = 0.05
    }
    SubShader
    {
        // Outline pass needs to be rendered in the opaque queue but before/after normal drawing depending on the effect
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+1" }
        LOD 100

        Pass
        {
            Name "Outline"
            // Cull Front is the trick for the inverted hull technique
            Cull Front
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Lỗi khoảng trống ở góc xảy ra vì Cube có các góc vuông (Hard Edges), 
                // Normal của các mặt bị tách rời nên khi push theo Normal các mặt sẽ bị bung ra.
                // Giải pháp: Phóng to vertex từ tâm dựa trên Vị trí (Position) thay vì Normal.
                
                // Dùng hàm sign() sẽ đẩy đỉnh ra 4 góc một cách đồng đều cho Cube / Plane
                float3 offset = sign(input.positionOS.xyz) * _OutlineThickness;
                
                // Extrude vertex ra ngoài
                float3 positionOS = input.positionOS.xyz + offset;
                
                output.positionHCS = TransformObjectToHClip(positionOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
