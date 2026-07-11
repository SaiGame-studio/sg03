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
                
                // The gap issue at the corners occurs because Cubes have Hard Edges.
                // The normals of the separated faces point in different directions, 
                // so pushing along the Normal causes the faces to detach and explode outwards.
                // Solution: Extrude the vertex outwards from the center based on its Position instead of Normal.
                
                // Using the sign() function evenly pushes the vertices outwards to all 4 corners for a Cube or Plane
                float3 offset = sign(input.positionOS.xyz) * _OutlineThickness;
                
                // Extrude vertex outward
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
