// CardHolder Selected — URP Unlit shader
// Draws a steady golden border glow plus an animated diagonal scan sweep.
// Apply to the CardHolder mesh (flat quad / plane).
Shader "SG03/CardHolder/Selected"
{
    Properties
    {
        _GlowColor    ("Glow Color",      Color)  = (1.0, 0.85, 0.2, 1.0)
        _ScanColor    ("Scan Color",      Color)  = (1.0, 1.0,  0.6, 0.6)
        _EdgeWidth    ("Edge Width",      Range(0.001, 0.5))  = 0.06
        _Intensity    ("Intensity",       Range(0.0, 8.0))    = 4.0
        _ScanWidth    ("Scan Band Width", Range(0.01, 0.5))   = 0.08
        _ScanSpeed    ("Scan Speed",      Range(0.0, 10.0))   = 1.8
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
            Name "CardHolderSelected"
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
                float4 _ScanColor;
                float  _EdgeWidth;
                float  _Intensity;
                float  _ScanWidth;
                float  _ScanSpeed;
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

                // ── Border glow ──────────────────────────────────────────
                float dx = min(uv.x, 1.0 - uv.x);
                float dy = min(uv.y, 1.0 - uv.y);
                float edgeDist = min(dx, dy);
                float border   = 1.0 - smoothstep(0.0, _EdgeWidth, edgeDist);

                half4 col   = _GlowColor;
                col.rgb    *= _Intensity;
                col.a       = border * _GlowColor.a;

                // ── Diagonal scan sweep ───────────────────────────────────
                // Diagonal value goes 0→2 in UV space; animate it over time
                float diag      = uv.x + uv.y;                       // [0, 2]
                float scanPos   = frac(_Time.y * _ScanSpeed * 0.3);  // [0, 1)
                float scanCentre = scanPos * 2.0;                    // [0, 2)
                float scanDist  = abs(diag - scanCentre);
                float scanMask  = 1.0 - smoothstep(0.0, _ScanWidth, scanDist);

                // Blend scan line on top of border
                float scanAlpha  = scanMask * _ScanColor.a;
                col.rgb = lerp(col.rgb, _ScanColor.rgb * _Intensity, scanMask * 0.5);
                col.a   = max(col.a, scanAlpha);

                return col;
            }
            ENDHLSL
        }
    }
}
