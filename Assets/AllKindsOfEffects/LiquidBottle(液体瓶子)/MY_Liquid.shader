Shader "URPNotes/MY_Liquid"{
    Properties{
        	_Plane("平面坐标", Vector) = (0,0,0,-1)
        	[HideInInspector]_PlanePos("平面坐标", Vector) = (0,0,0,-1)
        	[HDR]_Color("液体颜色", Color) = (1,1,1,1)
        	_BaseMap("Base Map(RGB:泡泡，A：浓稠度Mask)", 2D) = "white" {}
        	[HDR]_BaseColor("BaseColor基础颜色", Color) = (1, 1, 1, 1)
        	_ThickMaskStr("浓稠度Mask强度控制", Range(0, 2)) = 1
        	_GradualMaskStr("泡泡渐隐Mask强度控制", Range(0, 1)) = 1
        	_NormalMap("Normal Map", 2D) = "bump" {}
        	_WavesTex("Waves", 2D) = "black" {}
        	_Refraction("Refraction Index", Float) = 0.5
        	_TopColor("顶部颜色", Color) = (1,1,1,1)
        	_FoamColor("泡沫线颜色", Color) = (1,1,1,1)
        	_ProbeLod("Murkiness", Float) = 0.05
        	_Syrup("糖浆效果", Range(0, 1)) = 0
        	_EdgeThickness("边缘厚度", Float) = 0.02
        	_FresnelPower("菲涅尔强度", Float) = 1.5
        	_MeniscusHeight("弯月面高度", Float) = 0.04
        	_MeniscusCurve("弯月面弧度", Float) = 0.75
        	_FoamAmount("泡沫高度",  Range(0, 10)) = 1.0
        	_WavesScale("波浪大小", Float) = 1.0
        	[HideInInspector]_Foam("Foam", Float) = 1.0
        	_LiquidSpeed("液体速度(仅X,Y)", Vector) =(1,1,1,1)
        	_MainLightPower("受主光强度",Range(0, 1)) = 0.5
        	_Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        	[Header(Blending)]
        	// https://docs.unity3d.com/ScriptReference/Rendering.BlendMode.html
        	[Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("_SrcBlend (default = one)", Float) = 2 // 2 = one
        	[Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("_DstBlend (default = zero)", Float) = 3 // 2 = one

        	[HideInInspector]_BoundsL("Bounds L", float) = 0
        	[HideInInspector]_BoundsH("Bounds H", float) = 0
        	[HideInInspector]_BoundsX("Bounds X", float) = 0
        	[HideInInspector]_BoundsZ("Bounds Z", float) = 0
        	[HideInInspector]_MeshScale("Mesh Scale", float) = 0
        	[HideInInspector]_WavesMult("Waves Mult", float) = 0
        	[HideInInspector]_RimColor("RimColor", Color) = (1, 1, 1, 1)
        	[HideInInspector]_Center("Center", Float) = 0
        	[HideInInspector]_FresnelIntensity("FresnelIntensity", Float) = 0
        	[HideInInspector]surfNormal("surfNormal", Vector) = (0,0,0,0)
    }
    SubShader{
        Tags{
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalRenderPipeline"
        }
        pass{
        	cull off
            HLSLPROGRAM
                #pragma vertex Vertex
                #pragma fragment Pixel
				#pragma target 3.0
                
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);

                TEXTURE2D(_NormalMap);
                SAMPLER(sampler_NormalMap);
				TEXTURE2D(_BackgroundTexture);
				SAMPLER(sampler_BackgroundTexture);
				TEXTURE2D(_WavesTex);
				SAMPLER(sampler_WavesTex);
				// SAMPLER(sampler_LinearClamp);

                CBUFFER_START(UnityPerMaterial)
                    float _BumpScale;
                    float4 _MainTex_ST;
                    float4 _NormalMap_ST;
                    float _Gloss;
                    float _MainLightPower;
                    float4 _LiquidSpeed;
                    float _MeshScale;
                    float _WavesMult;
                    float _BoundsL;
                    float _BoundsH;
                    float _BoundsX;
                    float _BoundsZ;
                    float4 _BaseMap_ST;
                    float _MeniscusCurve;
                    float _WavesScale;
                    float4 _Plane;
                    float _GradualMaskStr;
                    float _EdgeThickness;
                    float _MeniscusHeight;
                    half3 surfNormal;
                    float _Refraction;
                    float _ProbeLod;
                    float _Syrup;
                    float _FoamAmount;
            	    float _Foam;
                    float4 _TopColor, _RimColor, _FoamColor, _Color, _BaseColor;
                    float _FresnelPower;
                    float _ThickMaskStr;
                CBUFFER_END

                #define UNITY_PI 3.1415926

                struct Attributes{

                    float4 positionOS:POSITION;
                    float3 normal:NORMAL;
                    float2 uv_MainTex:TEXCOORD0;
                    float2 uv_NormalMap:TEXCOORD1;
                    float4 tangentOS:TANGENT;
                    //注意tangent是float4类型，因为其w分量是用于控制切线方向的。
                };

                struct Varyings{
                    float4 positionCS:SV_POSITION;
                    float2 uv:TEXCOORD0;
                    float2 uv_NormalMap:TEXCOORD1;
                    float3 positionWS:TEXCOORD2;
                    float3 normalWS:TEXCOORD3;
                    float3 viewDirWS:TEXCOORD4;
                    float3 tSpace0:TEXCOORD5;
                    float3 tSpace1:TEXCOORD6;
                    float3 tSpace2:TEXCOORD7;
                };

				struct Triplanar
				{
				    float2 x, y, z;
				};
				
				Triplanar GetTriplanar(float3 worldPos)
				{
				    Triplanar tri;
				    tri.x = worldPos.zy;
				    tri.y = worldPos.xz;
				    tri.z = worldPos.xy;
				    return tri;
				}
				
				// XZ representation of a texture
				float4 BiplanarTex(TEXTURE2D_PARAM(tex, sampler_LinearClamp), float3 worldPos, float2 scale, float3 offset)
				{
				    float4 x = SAMPLE_TEXTURE2D(tex, sampler_LinearClamp, (worldPos.yz + offset.yz) * scale);
				    float4 z = SAMPLE_TEXTURE2D(tex, sampler_LinearClamp, (worldPos.xy + offset.xy) * scale);
				    return x + z;
				}

				float4 TriplanarTex(TEXTURE2D_PARAM(tex, sampler_LinearClamp), float3 worldPos, float3 normal, float2 scale,
                    float3 offset)
                {
                    normal = abs(normal);
                    float3 weights = normal / (normal.x + normal.y + normal.z);
                    float4 x = SAMPLE_TEXTURE2D(tex, sampler_LinearClamp, (worldPos.yz + offset.yz) * scale);
                    float4 y = SAMPLE_TEXTURE2D(tex, sampler_LinearClamp, (worldPos.xz + offset.xz) * scale);
                    float4 z = SAMPLE_TEXTURE2D(tex, sampler_LinearClamp, (worldPos.xy + offset.xy) * scale);
                    return weights.x * x + weights.y * y + weights.z + z;
                }
                
                float4 RotateAroundYInDegrees(float4 vertex, float degrees)
                {
                    float alpha = degrees * UNITY_PI / 180;
                    float sina, cosa;
                    sincos(alpha, sina, cosa);
                    float2x2 m = float2x2(cosa, sina, -sina, cosa);
                    return float4(vertex.yz, mul(m, vertex.xz)).xzyw;
                }
                
                float fresnelFunc(float3 viewDirection, float3 worldNormal)
                {
                    float powVal = 1.08 + dot(viewDirection, worldNormal);
                    return powVal * powVal * powVal * powVal * powVal * powVal * powVal * powVal * powVal * powVal;
                }
                
                float GetFresnel(float3 normal, float3 viewDir, float facing, float power, float intensity)
                {
                    float dotProduct = 1 - pow(saturate(dot(normal, normalize(facing * viewDir))), power) * intensity;
                    float fresnelCol = smoothstep(0.5, 1.0, dotProduct);
                    float fresnel = saturate(fresnelCol);
                    return fresnel;
                }


                float CalculateWaves(Varyings i, half facing)
                {
                    float fresnel = GetFresnel(i.normalWS, i.viewDirWS, facing, _MeniscusCurve, 0.5);
                    float4 wavesTex = BiplanarTex(
                        TEXTURE2D_ARGS(_WavesTex, sampler_WavesTex), i.normalWS, 0.25 / _MeshScale,
                        -_Time.xxx * 10 - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz);
                    float waves = saturate(wavesTex.r) - 0.5;
                    waves = waves * 0.005 * pow(_WavesMult, 5) * (1 + fresnel) * _MeshScale - (_WavesMult - 1) * 0.05;
                    //return waves;
                    return waves * _WavesScale;
                
                }
                
                // Calculates lighting for diffuse and specular, where a certain one
                // can be selected with "type" (0 - specular, 1 - diffuse)
                float3 GetLighting(bool type, float3 worldPos, float3 normal, float shininess)
                {
                    float3 lightDirection;
                    float attenuation;
                    float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos.xyz);
                    normal = normalize(normal);
                    if (_MainLightPosition.w == 0.0)
                    {
                        attenuation = 1.0;
                        lightDirection = normalize(_MainLightPosition.xyz);
                    }
                    else
                    {
                        float3 vertexToLightSource = _MainLightPosition.xyz - worldPos.xyz;
                        float liquidDistance = length(vertexToLightSource);
                        attenuation = 1.0 / liquidDistance;
                        lightDirection = normalize(vertexToLightSource);
                    }
                
                    float3 diffuseReflection = attenuation * _MainLightColor.rgb * max(0.0, dot(normal, lightDirection)) *
                        _MainLightPower;
                
                    shininess = clamp(shininess, 1, 1000);
                    float3 reflection = reflect(lightDirection, normal);
                    float3 specularReflection = pow(saturate(dot(reflection, -viewDir)), shininess);
                    specularReflection *= _MainLightColor;
                
                    if (type)
                        return specularReflection;
                    else
                        return diffuseReflection;
                }
                
                float3 GetLighting(float type, float3 worldPos, float3 normal)
                {
                    return GetLighting(type, worldPos, normal, 0);
                }
                
                float3 fresnelSchlickRoughness(float cosTheta, float3 F0, float roughness)
                {
                    return F0 + (max(float3(1, 1, 1) * (1 - roughness), F0) - F0) * pow(1.0 - cosTheta, 5.0);
                }
                
                // void override_Vertexing_Liquid(inout Attributes attributes, inout Varyings varyings)
                // {
                //     
                //
                //
                //     #if defined(_SUPPORT_ATHENA_FOG)
                //           varyings.fogFactor =  ComputeFogFactor( vertexInput.positionVS.z );
                //           TRANSFER_FOG_FACTOR(varyings.fogFactor, varyings.positionWS)
                //     #endif
                // }
                // #undef virtual_Vertexing
                // #define virtual_Vertexing override_Vertexing_Liquid
            
                Varyings Vertex(Attributes attributes){
                    Varyings varyings;
                    VertexPositionInputs vertexInput = GetVertexPositionInputs(attributes.positionOS.xyz);
                    VertexNormalInputs normalInput = GetVertexNormalInputs(attributes.normal, attributes.tangentOS);
                    varyings.uv = TRANSFORM_TEX(attributes.uv_MainTex, _MainTex);
                    varyings.positionWS = vertexInput.positionWS;
                    varyings.positionCS = vertexInput.positionCS;
                    //output.positionWS =mul(_ModelMatrix, varyings.positionOS);
                    half3 viewDirWS = GetWorldSpaceViewDir(varyings.positionWS);
                
                    varyings.normalWS = normalInput.normalWS;
                
                
                    varyings.viewDirWS = viewDirWS;
                
                    real sign = attributes.tangentOS.w * GetOddNegativeScale();
                    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
                    half3 worldBitangent = cross(normalInput.normalWS, tangentWS) * sign;
                
                    varyings.tSpace0 = half3(tangentWS.x, worldBitangent.x, normalInput.normalWS.x);
                    varyings.tSpace1 = half3(tangentWS.y, worldBitangent.y, normalInput.normalWS.y);
                    varyings.tSpace2 = half3(tangentWS.z, worldBitangent.z, normalInput.normalWS.z);
                    return varyings;
                }

                half4 Pixel(Varyings varyings,FRONT_FACE_TYPE VFace : FRONT_FACE_SEMANTIC):SV_TARGET{
                    //test
                    half facing = VFace;
                    half2 uv = 0;
                    float3 n = normalize(mul(UNITY_MATRIX_IT_MV, varyings.normalWS));
                
                    float r = sqrt(n.x * n.x + n.y * n.y + n.z * n.z);
                    uv = n.xy * 0.3;
                    float2 baseUV = frac(float2(_Time.x * _LiquidSpeed.x - uv.x, _Time.x * _LiquidSpeed.y - uv.y));
                    baseUV = baseUV.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
                
                    float finalAlpha = 0;
                
                    //浓稠度
                    float LiquidThick = 0;
                    half4 color= half4(0, 0, 0, 1);
                
                	
                    half4 BaseMapColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, baseUV);
                    LiquidThick = BaseMapColor.a;
                    BaseMapColor = BaseMapColor.r  *  _BaseColor;
                    BaseMapColor = BaseMapColor * _BaseColor.a;

                
                    
                    half4 colorAdd = half4(0, 0, 0, 0);
                
                    float fresnel = GetFresnel(varyings.normalWS, varyings.viewDirWS, facing, _MeniscusCurve, 0.5);
                    float height = (_BoundsH - _BoundsL);
                    

                    float waves = CalculateWaves(varyings, facing);

                
                    //Get cutoff plane
                    
                    float liquidDistance = dot(varyings.positionWS, _Plane.xyz);
                
                    liquidDistance += _Plane.w + waves / (_WavesMult + 1) / (_WavesMult + 1);
                
                    float gradualMask = pow(1-saturate((liquidDistance * LiquidThick+0.35*_GradualMaskStr)*10),1);
                    //DEBUG_OUTPUT(gradualMask , 1019004, "z");
                    
                
                    
                
                    // Meniscus
                    float increment = _EdgeThickness * 0.33;
                    float edgeOffset = fresnel * _MeniscusHeight + _EdgeThickness;
                    colorAdd = lerp(float4(0, 0, 0, 0), float4(0.35, 0.35, 0.35, 0),
                                    saturate((liquidDistance - edgeOffset + increment * 3) * 75));
                    colorAdd = lerp(colorAdd, float4(-0.35, -0.35, -0.35, 0), saturate((liquidDistance - edgeOffset + increment * 2.5) * 75));
                    colorAdd = lerp(colorAdd, float4(0, 0, 0, -0.5), saturate((liquidDistance - edgeOffset + increment * 1.6) * 75));
                    colorAdd = lerp(colorAdd, float4(0, 0, 0, -0.5), saturate((liquidDistance - edgeOffset + increment * waves * 20) * 75));
                    colorAdd = lerp(colorAdd, float4(0, 0, 0, -1), saturate((liquidDistance - edgeOffset + increment * waves * 25) * 75));
                    // Calculate normals
                    

                    float4 normalMap = BiplanarTex(
                        TEXTURE2D_ARGS(_NormalMap, sampler_NormalMap), varyings.positionWS, 1,
                        -mul(unity_ObjectToWorld, float4(0, 0, 0, 1)));
                    half3 tangentNormal;
                    if (facing > 0)
                        tangentNormal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, varyings.uv));
                    else
                        tangentNormal = UnpackNormal(normalMap);
                    half3 worldNormal;
                    worldNormal.x = dot(varyings.tSpace0, tangentNormal);
                    worldNormal.y = dot(varyings.tSpace1, tangentNormal);
                    worldNormal.z = dot(varyings.tSpace2, tangentNormal);

                
                    // Bubbles
                    // float4 bubbles;
                    // float bubbleDistance = saturate(liquidDistance * 3 + 1);
                    // float numBubbles = _BubbleCount * (_WavesMult/2 - 1);
                    // numBubbles = clamp(numBubbles, 0, _BubbleCount);

                    
                
                    surfNormal = worldNormal;
                    half3 topNormal = normalize(half3(_Plane.x + waves * 10, _Plane.y, _Plane.z + waves * 10));
                    if (facing < 0)
                    {
                        // If backface, make its normal face up
                        surfNormal = topNormal;
                        surfNormal = lerp(surfNormal, -worldNormal, saturate((liquidDistance - edgeOffset + increment * 3) * 25));
                    }
                    else
                    {
                        // If front face, make the surface edge's normal lerp to up
                        surfNormal = lerp(surfNormal, topNormal, saturate((liquidDistance - edgeOffset + increment * 3) * 25));
                        surfNormal = lerp(surfNormal, worldNormal, saturate((liquidDistance - edgeOffset + _EdgeThickness / 3) * 100));
                        // surfNormal.x *= (bubbles.rgb * bubbleDistance * 4 + 1);
                        // surfNormal.y *= (bubbles.rgb * bubbleDistance * 4 + 1);
                        // surfNormal.z *= (bubbles.rgb * bubbleDistance * 4 + 1);
                    }
                
                    // Meniscus gets some extra refraction
                    _Refraction = lerp(_Refraction, _Refraction + 0.5, saturate((liquidDistance - edgeOffset + increment * 3) * 25)) *
                        _LiquidSpeed.x;
                
                    float3 refractedDirection = refract(-normalize(varyings.viewDirWS), surfNormal, 1.0 / _Refraction);
                    float3 reflectedDirection = reflect(varyings.viewDirWS, normalize(surfNormal));
                    float4 envCubeRefract = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, refractedDirection,
                                                                   _ProbeLod * UNITY_SPECCUBE_LOD_STEPS);
                    float4 envCubeReflect = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, -reflectedDirection,
                                                                   _ProbeLod * UNITY_SPECCUBE_LOD_STEPS);
                    half3 refraction = DecodeHDREnvironment(envCubeRefract, unity_SpecCube0_HDR);
                    half3 reflection = DecodeHDREnvironment(envCubeReflect, unity_SpecCube0_HDR);
                
                    color.rgb *= refraction * (1 - _Syrup);
                    color.rgb += _Syrup;
                
                    // Calculate lighting stuff
                    float shininess = 30 * (1 - _ProbeLod);
                    float3 specularReflection = GetLighting(false, varyings.positionWS, surfNormal, shininess);
                    float3 diffuseReflection = GetLighting(true, varyings.positionWS, surfNormal);
                    float3 ambientLighting = UNITY_LIGHTMODEL_AMBIENT.rgb;
                
                    _Foam = clamp(_Foam, 0, _FoamAmount * 0.03);
                    

                            float4 Wavesnoise = TriplanarTex(
                            TEXTURE2D_ARGS(_WavesTex, sampler_WavesTex), varyings.positionWS, surfNormal, float2(1, 1), float3(0, waves, 0));

                
                    float F_B_Alpha = 0;
                    if (facing > 0)
                    {
                        //伪次表面散射
                        
                        //归一化法线
                        float3 normalPBR= normalize(varyings.normalWS);
                        //光照方向
                        //float3 lightDirPBR = normalize(_MainLightPosition.xyz);
                        //视线方向
                        float3 viewDirPBR =  normalize(_WorldSpaceCameraPos - varyings.positionWS.xyz);
                        
                        float nv = max(saturate(dot(normalPBR,viewDirPBR)),0.000001);
                        //float nl = max(saturate(dot(normalPBR,lightDirPBR)),0.000001);
                        
                        color.rgb += (1-(nv*0.5+0.5)) * _Color + _Color * 0.1;
                
                        float3 hsv = RgbToHsv(_Color.rgb);
                        hsv.g *=1.5;
                        hsv.b *=5;
                        
                        // If front face
                        float refresnel = GetFresnel(varyings.normalWS, varyings.viewDirWS, facing, _FresnelPower, 1);
                        color.rgb *= (1 - refresnel);
                        color.rgb += reflection * refresnel;
                        color.rgb *= _Color;
                        color += colorAdd;
                        color.rgb += (specularReflection * float4(HsvToRgb(hsv),_Color.a));
                        //color.rgb -= bubbles.a / 4 * bubbleDistance;
                        //color.rgb += saturate(bubbles.rgb) / 4 * bubbleDistance;
                        color = lerp(color, _FoamColor * float4((diffuseReflection + ambientLighting) * Wavesnoise * 0.25 + 0.5, 1),
                                     saturate((liquidDistance - edgeOffset + _EdgeThickness / 3) * 100) * saturate(_Foam * 100));
                        color = lerp(color, float4(color.r, color.g, color.b, 0),
                                     saturate((liquidDistance / 6 - _Foam - edgeOffset / 6 + _EdgeThickness / 18) * 600));
                        
                        finalAlpha = saturate(saturate((liquidDistance - edgeOffset + _EdgeThickness / 3) * 100) * saturate(_Foam * 100) + saturate((liquidDistance / 6 - _Foam - edgeOffset / 6 + _EdgeThickness / 18) * 600));
                
                        //加上baseMap的渐变遮罩
                        color.rgb = color.rgb + BaseMapColor.rgb * (step(finalAlpha,0)) * gradualMask;
                
                        // hsv = RgbToHsv(color);
                        // hsv.g = LiquidThick*hsv.g;
                        // color.rgb = HsvToRgb(hsv);
                        
                
                        //正反面透明度区分
                        F_B_Alpha = saturate(_Color.a-_Color.a * (LiquidThick) * _ThickMaskStr);
                    }
                    else
                    {
                        // If back face, act as liquid's surface
                        color.rgb = _TopColor;
                        float bfFresnel = pow(1 + dot(-normalize(varyings.viewDirWS), _Plane), _FresnelPower * 0.35);
                        color.rgb *= (1 - bfFresnel);
                        //color.rgb += reflection * bfFresnel;
                        //color.rgb *= _TopColor;
                        color = lerp(color, float4(color.r, color.g, color.b, 0),
                                     saturate((liquidDistance - edgeOffset + increment * 1.6) * 100));
                        color.rgb += specularReflection * bfFresnel;
                        color.a = lerp(color.a, 1, saturate((liquidDistance - edgeOffset + _EdgeThickness) * 100));
                        color.a = lerp(color.a, 0, saturate((liquidDistance / 6 - _Foam - edgeOffset / 6 + _EdgeThickness / 18) * 600));
                        
                        color.rgb = lerp(color, _FoamColor * (bfFresnel * 0.5 + 0.5), saturate(_Foam * 100));
                        
                        //正反面透明度区分
                        F_B_Alpha = saturate(_Color.a + 0.2);
                        
                    }
                    
                    
                    // color *= _BaseColor;
                    if (color.a <= 0) discard;
                
                    float3 finalColor = color.rgb;
                    //泡沫和液体的透明度区分
                    finalAlpha = lerp(F_B_Alpha,_FoamColor.a,finalAlpha);

                    //test


                    
                    return half4(finalColor,finalAlpha);
                }
            ENDHLSL
        }
    }
}