Shader "SG03/ArrowIndicator"
{
    Properties
    {
        _Color       ("Base Color",   Color)          = (1, 0.55, 0.05, 1)
        _GlowColor   ("Glow Color",   Color)          = (1, 0.9, 0.3, 1)
        _DashLength  ("Dash Length",  Range(0.02, 1)) = 0.25
        _GapRatio    ("Gap Ratio",    Range(0, 0.95)) = 0.45
        _FlowSpeed   ("Flow Speed",   Float)          = 2.5
        _PulseSpeed  ("Pulse Speed",  Float)          = 4.0
        _PulseAmount ("Pulse Amount", Range(0, 0.5))  = 0.2
        _GlowWidth   ("Glow Width",   Range(0, 1))    = 0.5
        _EdgeFade    ("Edge Fade",    Range(0.01, 0.5)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay+1" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float4 color : COLOR;
                float2 uv    : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _GlowColor;
            float  _DashLength;
            float  _GapRatio;
            float  _FlowSpeed;
            float  _PulseSpeed;
            float  _PulseAmount;
            float  _GlowWidth;
            float  _EdgeFade;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv    = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // --- Flowing dashes along U (0 = source, 1 = destination) ---
                float u          = i.uv.x - _Time.y * _FlowSpeed;
                float cycle      = fmod(abs(u), _DashLength);
                float dashSolid  = _DashLength * (1.0 - _GapRatio);
                float dashMask   = step(cycle, dashSolid);

                // --- Soft edge fade along V (V=0 and V=1 are edges, V=0.5 is center) ---
                float vCentered = abs(i.uv.y - 0.5) * 2.0;   // 0 at center, 1 at edge
                float edgeFade  = 1.0 - smoothstep(1.0 - _EdgeFade * 2.0, 1.0, vCentered);

                // --- Glow at center of line ---
                float glow      = 1.0 - smoothstep(0.0, _GlowWidth, vCentered);

                // --- Pulse brightness ---
                float pulse     = 1.0 - _PulseAmount + _PulseAmount * sin(_Time.y * _PulseSpeed);

                // --- Combine ---
                fixed4 col  = lerp(_Color, _GlowColor, glow) * i.color;
                col.a       = _Color.a * dashMask * edgeFade * pulse;

                return col;
            }
            ENDCG
        }
    }
}
