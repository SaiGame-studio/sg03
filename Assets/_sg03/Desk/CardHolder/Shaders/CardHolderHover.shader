// CardHolder Hover — URP Unlit shader
// Draws an animated pulsing border glow using UV-edge distance.
// Apply to the CardHolder mesh (flat quad / plane).
Shader "SG03/CardHolder/Hover"
{
    Properties
    {
        _GlowColor   ("Glow Color",      Color)  = (0.3, 0.9, 1.0, 1.0)
        _EdgeWidth   ("Edge Width",      Range(0.001, 0.5)) = 0.06
        _Intensity   ("Intensity",       Range(0.0, 8.0))   = 3.0
        _PulseSpeed  ("Pulse Speed",     Range(0.0, 10.0))  = 2.5
        _PulseMin    ("Pulse Min",       Range(0.0, 1.0))   = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Transparent"
        }

        Pass
        {
            Name "CardHolderHover"
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
                float  _EdgeWidth;
                float  _Intensity;
                float  _PulseSpeed;
                float  _PulseMin;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // Distance to nearest UV edge (0 at border, 0.5 at center)
                float dx = min(uv.x, 1.0 - uv.x);
                float dy = min(uv.y, 1.0 - uv.y);
                float edgeDist = min(dx, dy);

                // Border mask: bright at edge, zero in interior
                float border = 1.0 - smoothstep(0.0, _EdgeWidth, edgeDist);

                // Animated pulse: oscillates between _PulseMin and 1
                float pulse = _PulseMin + (1.0 - _PulseMin)
                              * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed));

                float alpha = border * pulse;

                half4 col = _GlowColor;
                col.rgb *= _Intensity;
                col.a    = alpha * _GlowColor.a;

                return col;
            }
            ENDHLSL
        }
    }
}
