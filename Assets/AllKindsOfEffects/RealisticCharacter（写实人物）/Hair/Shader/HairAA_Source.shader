Shader "HairAA/Source"
{
    Properties
    {
//        [Tex(_MainColor)]_MainTex ("Main Tex", 2D) = "white" { }
//        [HideInInspector]_MainColor ("Main Color", Color) = (1, 1, 1, 1)
        _RIDO("RIDO", 2D) = "white" { }
        _CutoffMaxDistance("Max Distance", float) = 10
        
        [Foldout(1, 1, 0, 0)]_Other ("Other_Foldout", float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        half4 _MainColor;
        half _CutoffMaxDistance;
        CBUFFER_END
        ENDHLSL
        
        Pass
        {
            Name "HairAASource"
            Tags { "LightMode" = "HairAA_Source" }

            // -------------------------------------
            // Render State Commands
            ZWrite on
            ZTest lequal
            Cull off

            HLSLPROGRAM
            #pragma target 2.0
            
            #pragma vertex vert
            #pragma fragment frag
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
                float4 tangentOS:TANGENT;
                float3 normalOS: NORMAL;
            };
            
            struct v2f
            {
                float4 vertex: SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 worldPos: TEXCOORD1;
                float4 tangentWS:TANGENT;
                float4 normalWS:NORMAL;
                float4 BtangentWS:TEXCOORD2;
                float3 TangentSS:TEXCOORD3;
                float3 BtangentSS:TEXCOORD4;
            };
            
            TEXTURE2D(_RIDO);
            SAMPLER(sampler_RIDO);
            // TEXTURE2D(_MainTex);
            // SAMPLER(sampler_MainTex);
            
            v2f vert(appdata v)
            {
                v2f o;
                VertexPositionInputs vertexPos = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexPos.positionCS;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS.xyz=normalize(TransformObjectToWorldNormal(v.normalOS.xyz));
                float3 normalSS = normalize(TransformWorldToViewNormal(o.normalWS.xyz));
                o.tangentWS.xyz=normalize(TransformObjectToWorldDir(v.tangentOS.xyz));
                o.TangentSS = normalize(TransformWorldToViewDir(o.tangentWS.xyz));
                o.BtangentWS.xyz=cross(o.normalWS.xyz,o.tangentWS.xyz)*v.tangentOS.w*unity_WorldTransformParams.w;
                o.BtangentSS = normalize(TransformWorldToViewDir(o.BtangentWS.xyz));
                
                o.uv = v.uv;
                return o;
            }
            
            float4 frag(v2f i): SV_Target
            {
                float4 RIDO = SAMPLE_TEXTURE2D(_RIDO, sampler_RIDO, i.uv);
                // half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                float distance = length(i.worldPos - _WorldSpaceCameraPos);
                float dynamicCutoff = lerp(0.5, 0.05, saturate(distance / _CutoffMaxDistance));
                // float4 BtangentSS = mul(UNITY_MATRIX_VP, float4(i.BtangentWS.xyz, 0));
                // float4 TangentSS = mul(UNITY_MATRIX_VP, float4(i.tangentWS.xyz,0));
                clip(RIDO.a - dynamicCutoff);
                // return float4(RIDO.xxx, 1);
                return float4((i.BtangentSS.xy)*0.5+0.5,0, 1);
            }
            ENDHLSL            
        }
    }
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
