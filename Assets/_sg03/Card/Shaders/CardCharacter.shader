// Simple alpha-cutout shader for the card character quad.
// Hardcodes Cull Back, ZWrite On, ZTest LEqual, AlphaTest queue so it reliably
// writes to the depth buffer and occludes all transparent shaders behind it.
Shader "SG03/CardCharacter"
{
    Properties
    {
        _MainTex ("Texture", 2D)             = "white" {}
        _Cutoff  ("Alpha Cutoff", Range(0,1)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "AlphaTest"
            "RenderType"      = "TransparentCutout"
            "IgnoreProjector" = "True"
        }

        Cull    Back
        ZWrite  On
        ZTest   LEqual
        Lighting Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float     _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - _Cutoff);
                return col;
            }
            ENDCG
        }
    }

    FallBack "Legacy Shaders/Transparent/Cutout/Diffuse"
}
