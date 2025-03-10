// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "LongNote/Line-Outline" 
{
    Properties
    {
        _Texture0("Texture0 (RGB) Trans (A)", 2D) = "clear" {}
        _Texture1("Texture1 (RGB) Trans (A)", 2D) = "clear" {}
        _Texture2("Texture2 (RGB) Trans (A)", 2D) = "clear" {}
        _OutlineWidth("Outline Width", float) = 1
        _OutlineCutoff("Outline Cutoff", Range(0, 1)) = 0.5
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            { 
                float4 vertex : SV_POSITION;
                half2 texcoord0 : TEXCOORD0;
                half2 texcoord1 : TEXCOORD1;
                half2 texcoord2 : TEXCOORD2;
                float4 color : COLOR;
            };

            sampler2D _Texture0;
            sampler2D _Texture1;
            sampler2D _Texture2;
            half _OutlineWidth;
            half _OutlineCutoff;
            fixed4 _OutlineColor;
            float4 _Texture0_ST;
            float4 _Texture1_ST;
            float4 _Texture2_ST;
            float4 _Texture0_TexelSize;

            v2f vert(appdata_t v)
            {
                v2f o; 
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord0 = TRANSFORM_TEX(v.texcoord, _Texture0);
                o.texcoord1 = TRANSFORM_TEX(v.texcoord, _Texture1);
                o.texcoord2 = TRANSFORM_TEX(v.texcoord, _Texture2);
                o.color = v.color;
                return o;
            }

            fixed4 SampleBlend(v2f i)
            {
                fixed4 _0 = tex2D(_Texture0, i.texcoord0);
                fixed4 _1 = tex2D(_Texture1, i.texcoord1);
                fixed4 _2 = tex2D(_Texture2, i.texcoord2);

                fixed4 blend01 = lerp(_0, _1, _1.a);
                fixed4 blend012 = lerp(blend01, _2, _2.a);
                return blend012;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                const float DIV_SQRT_2 = 0.70710678118;
                float2 directions[8] =
                {
                    float2(1, 0), float2(0, 1), float2(-1, 0), float2(0, -1),
                    float2(DIV_SQRT_2, DIV_SQRT_2), float2(-DIV_SQRT_2, DIV_SQRT_2),
                    float2(-DIV_SQRT_2, -DIV_SQRT_2), float2(DIV_SQRT_2, -DIV_SQRT_2)
                };

                float2 sampleDistance = _Texture0_TexelSize.xy * _OutlineWidth;
                float sumAlpha = 0;
                for (uint index = 0; index < 8; ++index)
                {
                    float2 sampleUV = i.texcoord0 + directions[index] * sampleDistance;
                    sumAlpha += tex2D(_Texture0, sampleUV).a;
                }
                float outline = sumAlpha > _OutlineCutoff;

                fixed4 color = tex2D(_Texture0, i.texcoord0);
                return lerp(color * i.color, outline * _OutlineColor, color.a < _OutlineCutoff);
            }
            ENDCG
        }
    }
}