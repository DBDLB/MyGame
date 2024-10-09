Shader "Unlit/Face Orientation"
{
    Properties
    {
        _ColorFront ("Front Color", Color) = (1,0.7,0.7,1)
        _ColorBack ("Back Color", Color) = (0.7,1,0.7,1)
    }
    SubShader
    {
        Pass
        {
            Cull Off // 关闭背面剔除

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

                
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma target 3.0

            struct Varyings
            {
                half facing : VFACE;
            };

            float4 vert (float4 vertex : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(vertex.xyz);
            }

            half4 _ColorFront;
            half4 _ColorBack;

            half4 frag (Varyings varyings) : SV_Target
            {
                // 正面的 VFACE 输入为正，
                // 背面的为负。根据这种情况
                // 输出两种颜色中的一种。
                return varyings.facing > 0 ?_ColorFront : _ColorBack;
            }
            ENDHLSL
        }
    }
}