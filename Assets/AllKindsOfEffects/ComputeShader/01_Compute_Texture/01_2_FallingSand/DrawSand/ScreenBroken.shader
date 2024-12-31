Shader "Unlit/ScreenBroken"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScreenBrokenNormal("屏幕破碎法线图", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

			sampler2D _ScreenBrokenNormal;
			float4 _ScreenBrokenNormal_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
	        //利用碎屏法线对 基础UV进行偏移 
                float2 bump = UnpackNormal(tex2D(_ScreenBrokenNormal, i.uv)).rg;
		i.uv = bump * 0.5 + i.uv;
                half4 col = tex2D(_MainTex, i.uv);
				
                return col;
            }
            ENDHLSL
        }
    }
}