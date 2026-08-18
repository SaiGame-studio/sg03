// CardHolder Default — URP Unlit shader
// Draws a small pulsing glow dot at the center of the holder.
Shader "SG03/CardHolder/Default"
{
    Properties
    {
        _GlowColor  ("Glow Color",    Color)           = (0.6, 0.8, 1.0, 1.0)
        _DotRadius  ("Dot Radius",    Range(0.01, 0.5)) = 0.08
        _Softness   ("Softness",      Range(0.001, 0.3)) = 0.06
        _Intensity  ("Intensity",     Range(0.0, 8.0))   = 2.5
        _PulseSpeed ("Pulse Speed",   Range(0.0, 10.0))  = 1.5
        _PulseMin   ("Pulse Min",     Range(0.0, 1.0))   = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "CardHolderDefault"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float  _DotRadius;
                float  _Softness;
                float  _Intensity;
                float  _PulseSpeed;
                float  _PulseMin;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Only render on upward-facing surfaces — discard all side/bottom faces of a Cube
                clip(IN.normalWS.y - 0.5);

                // Distance from UV center
                float2 centered = IN.uv - 0.5;
                float  dist     = length(centered);

                // Hard discard every pixel outside the dot radius — prevents any edge bleed
                clip((_DotRadius + _Softness) - dist);

                // Soft falloff inside the dot
                float dotMask = 1.0 - smoothstep(_DotRadius - _Softness, _DotRadius + _Softness, dist);

                // Pulsing scale: oscillates between _PulseMin and 1
                float pulse = _PulseMin + (1.0 - _PulseMin)
                              * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed));

                half4 col = _GlowColor;
                col.rgb  *= _Intensity;
                col.a     = dotMask * pulse * _GlowColor.a;

                return col;
            }
            ENDHLSL
        }
    }
}
