Shader "Custom/ShallowDepth" {

    Properties
    {
    }
    
    HLSLINCLUDE

    inline float2 EncodeFloatRG( float v )
    {
        float2 kEncodeMul = float2(1.0, 255.0);
        float kEncodeBit = 1.0/255.0;
        float2 enc = kEncodeMul * v;
        enc = frac (enc);
        enc.x -= enc.y * kEncodeBit;
        return enc;
    }

    struct appdata_base {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float4 texcoord : TEXCOORD0;
    };

    
    struct v2f {
        float4 vertex : SV_POSITION;
        float4 texPos : TEXCOORD0;
    };

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
    v2f vert(appdata_base v)
    {
        v2f o;
        o.vertex = TransformObjectToHClip(v.vertex);
        float z = mul(UNITY_MATRIX_MV, v.vertex).z;
        o.texPos.z = z;
        return o;
    }

    uniform float4 _ShallowWaterParams;

    float4 frag(v2f i) : SV_Target
    {
        float depth = i.texPos.z;
        depth = clamp(-depth, 0, _ShallowWaterParams.z);
        depth = depth / _ShallowWaterParams.z;
        return float4(EncodeFloatRG(depth), 0, 1);
    }
    
    ENDHLSL

    SubShader{
        Tags{ "RenderType" = "Opaque" }
        CULL Off
        Pass{
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }
    }
}