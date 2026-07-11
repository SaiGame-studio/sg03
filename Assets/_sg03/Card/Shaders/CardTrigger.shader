// URP Unlit shader for Card Trigger (Additive Glow)
Shader "SG03/Card/CardTrigger"
{
    Properties
    {
        _MainTex ("Texture (Mask)", 2D) = "white" {}
        _IsTrigger ("Is Trigger (0=False, 1=True)", Range(0, 1)) = 0
        [HDR] _TriggerColor ("Trigger Color (Red)", Color) = (1.0, 0.0, 0.0, 1.0)
        [HDR] _NonTriggerColor ("Non-Trigger Color (Blue/Green)", Color) = (0.0, 1.0, 0.0, 1.0)
        _Intensity ("Emission Intensity", Range(0.0, 10.0)) = 2.0
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
            Name "CardTrigger"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _IsTrigger;
                float4 _TriggerColor;
                float4 _NonTriggerColor;
                float  _Intensity;
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
                OUT.uv          = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 emitCol = lerp(_NonTriggerColor, _TriggerColor, _IsTrigger);
                
                half4 finalCol;
                finalCol.rgb = texColor.rgb * emitCol.rgb * _Intensity;
                finalCol.a   = texColor.a * emitCol.a;
                return finalCol;
            }
            ENDHLSL
        }
    }
}
