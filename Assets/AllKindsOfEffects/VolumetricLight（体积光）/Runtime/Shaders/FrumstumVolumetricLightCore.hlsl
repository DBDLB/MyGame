#ifndef FrustumVolumetricLightCore
#define FrustumVolumetricLightCore

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID  
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float4 positionSS : TEXCOORD1;
};

struct FragOutput
{
    float4 color : SV_Target0;
#if defined(_VOLUMETRIC_LIGHT_SHADOW_ENABLE)
    float2 motion : SV_Target1;
#endif
};


// noise
TEXTURE2D(_VolumetricLightNoiseTexture); SAMPLER(sampler_VolumetricLightNoiseTexture);


// depth
TEXTURE2D_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
            
// shadow todo 暂时没有
TEXTURE2D_FLOAT(_VolumetricLightDepth);
SAMPLER(sampler_VolumetricLightDepth);

CBUFFER_START(UnityPerMaterial)
float4 _VolumetricLightNoiseTexture_TexelSize;
    float4x4 _LightPreVP;
    float4 _BoundaryPlanes_0;
    float4 _BoundaryPlanes_1;
    float4 _BoundaryPlanes_2;
    float4 _BoundaryPlanes_3;
    float4 _BoundaryPlanes_4;
    float4 _BoundaryPlanes_5;

    // ray marching
    int _Steps;

    // scattering
    float _AbsorptionRatio;
    float _HGFactor;
    float _TransmittanceExtinction;
    float _IncomingLoss;
    float _OutScatteringLoss;
    float _DistanceFalloffCoe;
    float _Corner;
    float _Outset;
    float _Smooth;

    // light
    float4 _LightColor;
    float _LightIntensity;

    float4 _LightPosition;
    float4 _LightDirection;

float4 _ShadowVMatrix_0;
float4 _ShadowVMatrix_1;
float4 _ShadowVMatrix_2;
float4 _ShadowVMatrix_3;

float _HGPhaseCurve;
float4 _VolumetricLightDepth_HDR;
CBUFFER_END




float4 TransformWorldToShadow(float3 positionWS)
{
    float4x4 m;
    m[0] = _ShadowVMatrix_0;
    m[1] = _ShadowVMatrix_1;
    m[2] = _ShadowVMatrix_2;
    m[3] = _ShadowVMatrix_3;
    float4 shadowCoord = mul(m, float4(positionWS, 1.0));
    shadowCoord = shadowCoord / shadowCoord.w;
    return shadowCoord;
}
            
#if !defined(SHADER_API_MOBILE)
#define IS_PC 1
#endif
#if defined(IS_PC)
        #define _MAX_STEP 256
#else
        #define _MAX_STEP 16
#endif

        inline half3 DecodeHDR(half4 data, half4 decodeInstructions)
{
    half alpha = decodeInstructions.w * (data.a - 1.0) + 1.0;

    #if defined(UNITY_COLORSPACE_GAMMA)
        return (decodeInstructions.x * alpha) * data.rgb;
    #else
        #if defined(UNITY_USE_NATIVE_HDR)
            return decodeInstructions.x * data.rgb;
        #else
            return (decodeInstructions.x * pow(alpha,decodeInstructions.y))*data.rgb;
    #endif
    #endif
}
            
        float GetMaxDepthFromTexture(float2 screenUV)
        {
            float depth = DecodeHDR(SAMPLE_DEPTH_TEXTURE_LOD(_VolumetricLightDepth,sampler_VolumetricLightDepth,screenUV,0),_VolumetricLightDepth_HDR);
            return 1-depth;
        }

#define CLIP_POS 1
#define DEPTH_TEXTURE 1
   		half CalculateInCameraDepth(float3 pos)
   		{
#if DEPTH_TEXTURE
   			float4 clipPos = TransformWorldToShadow(pos);
   			half clipZ = 0;
   			if (clipPos.z < 1 + HALF_EPS && clipPos.z + HALF_EPS > 0 && clipPos.x + HALF_EPS > 0 && clipPos.x < CLIP_POS + HALF_EPS && clipPos.y + HALF_EPS > 0 && clipPos.y < CLIP_POS + HALF_EPS)
   				clipZ = 1;
   			else
   				return 0;
   			float depthBake = GetMaxDepthFromTexture(clipPos.xy);
    
        #if UNITY_REVERSED_Z
            float z = 1 - clipPos.z;
        #else
            float z = clipPos.z;
        #endif
        return z < depthBake;

#endif
   			return 1;
   		}

        float GetWorldDepth(float3 positionWS, float cameraDepth)
        {
            float3 worldSpaceVec = positionWS - GetCameraPositionWS();
            float viewSpaceZ = TransformWorldToViewDir(worldSpaceVec).z;
            worldSpaceVec *= -LinearEyeDepth(cameraDepth, _ZBufferParams) / viewSpaceZ;
            return length(worldSpaceVec);
        }

        float intersectPlane(float4 plane, float3 origin, float3 dir, out bool intersect)
        {
            // t = -(O . P) / (D . P) = minDistance / cos
            float d = dot(dir, plane.xyz);
            intersect = d != 0;
            return -dot(float4(origin.xyz, 1), plane) / d;
        }

        float4 TransformObjectToWorldPlane(float4 plane)
        {
            float4 planeWS = mul(plane, GetWorldToObjectMatrix());
            return planeWS;
        }
        //AABB 求最大入，最小出，获得距离
        float getBoundary(float3 ray, out float near, out float far)
        {
            float maxNear = _ProjectionParams.y;
            float minFar = _ProjectionParams.z;
            bool intersected = false;
            float4 boundaryPlanes[6];
            boundaryPlanes[0] = _BoundaryPlanes_0;
            boundaryPlanes[1] = _BoundaryPlanes_1;
            boundaryPlanes[2] = _BoundaryPlanes_2;
            boundaryPlanes[3] = _BoundaryPlanes_3;
            boundaryPlanes[4] = _BoundaryPlanes_4;
            boundaryPlanes[5] = _BoundaryPlanes_5;
            [unroll]
            for(int i = 0; i < 6; i++)
            {
                float4 planeWS = TransformObjectToWorldPlane(boundaryPlanes[i]);
                float t = intersectPlane(planeWS, GetCameraPositionWS(), ray, intersected);
                if(intersected && dot(ray, planeWS.xyz) < 0) // frontface
                    maxNear = max(maxNear, t);
                else if(intersected)
                    minFar = min(minFar, t);
            }
            near = maxNear;
            far = minFar;
            return minFar - maxNear;
        }

        float sampleOffset(float2 screenPos)
        {
            return SAMPLE_TEXTURE2D(_VolumetricLightNoiseTexture, sampler_VolumetricLightNoiseTexture, screenPos * _ScreenParams.xy * _VolumetricLightNoiseTexture_TexelSize.xy);
        }
        
        float phaseHG(float cos, float g)
        {
            return (1 - g * g) / (4 * PI * pow(1 + g * g - 2 * g * cos, 1.5)); 
        }

        float phaseSchlick(float cos, float g)
        {
            float k = 1.55f * g - 0.55f * g * g;
            return (1 - k * k) / (4 * PI * (1 + k * cos) * (1 + k * cos));
        }

        float phaseIso(float cos, float g)
        {
            return 1 / (4 * PI);
        }

        float phase(float3 lightDir, float3 viewDir, float g)
        {
            float vDotl = dot(viewDir, lightDir);
            return phaseIso(vDotl, g);
        }

        float extinctionAt(float3 pos)
        {
            return 1 * _TransmittanceExtinction;
        }

        float sigmaSAt(float3 pos)
        {
            return extinctionAt(pos);// - _AbsorptionRatio;
        }

        float3 GetLightPositionWS(float3 pos, float lightWidth, float lightHeight)
        {
            float3 lightPos = float3(0, _LightPosition.y / _LightPosition.w, 0);
            float3 lightPosWS = TransformObjectToWorld(lightPos);
            return lightPosWS;
    #if defined(_CONE_VOLUMETRIC_LIGHT)
        return TransformObjectToWorld(float3(0, 0, 0));
    #else
            // emm..
            float2 lightRect = float2(lightWidth, lightHeight);
            float3 positionOS = TransformWorldToObject(pos);
            positionOS.xz = clamp(positionOS.xz, -lightRect.xy * 0.5f, lightRect.xy * 0.5);
            positionOS.y = 0.01;
            float3 lightPositionWS = TransformObjectToWorld(positionOS);
        return lightPositionWS;
    #endif
        }

        float2 GetObjectPreScreenUV(float3 nearPositionWS, float3 farPositionWS)
        {
            float4 previousScreenUV = mul(_LightPreVP, float4(nearPositionWS, 1));
            previousScreenUV = ComputeScreenPos(previousScreenUV);
            previousScreenUV /= previousScreenUV.w;
            if (previousScreenUV.z < 0 || previousScreenUV.z > 1)
            {
            	previousScreenUV = mul(_LightPreVP, float4(farPositionWS, 1));
            	previousScreenUV = ComputeScreenPos(previousScreenUV);
				previousScreenUV /= previousScreenUV.w;
            }
            return previousScreenUV.xy;
        }
        //体积阴影计算
        float shadowAt(float3 pos)
        {
        #if defined(_VOLUMETRIC_LIGHT_SHADOW_ENABLE)
            return CalculateInCameraDepth(pos);
        #else
            return 1;
        #endif
        }

        float3 volShadAt(float3 pos, float3 lightPos)
        {
            // to do: ray marching
            float d = distance(pos, lightPos);
            float sigmE = extinctionAt((pos + lightPos) * 0.5);
            float t = exp(- sigmE * d);
            t = lerp(1, t, _IncomingLoss);
            return t;
        }
        // 可见性计算
        float3 visibilityAt(float3 pos, float3 lightPos)
        {
            return shadowAt(pos) * volShadAt(pos, lightPos);
        }

        float lightDirectionFalloff(float3 pos, float3 direction)
        {
            float3 positionOS = TransformWorldToObject(pos);
            float width = _LightPosition.x;
            float height = _LightPosition.y;
            float hTan = _LightPosition.w;
                    
            float planeDepth = - positionOS.y;
            float planeh = planeDepth * hTan + 0.5f * height;
            float wTan = hTan * width / (height + HALF_EPS);
            float planew = planeDepth * wTan + 0.5f * width;
            float hPos = abs(positionOS.z);
            float wPos = abs(positionOS.x);
            float hRatio = (max(hPos, 0.5f * height) - 0.5f * height) / (planeh - 0.5f * height + HALF_EPS);
            float wRatio = (max(wPos, 0.5f * width) - 0.5f * width) / (planew - 0.5f * width + HALF_EPS);
            float t = (min(cos(hRatio * PI * 0.5), cos(wRatio * PI * 0.5)));
            return t;
    
        #if defined(_CONE_VOLUMETRIC_LIGHT)
            float3 lightDirection = half3(0, -1, 0);
            lightDirection = normalize(TransformObjectToWorldDir(lightDirection));
            return clamp(dot(direction, lightDirection), 0, 1);
        #else
            return 1;
        #endif
        }


        float AttenuationToZero(float d)
        {
            // attenuation = 1 / (1 + distance_to_light / light_radius)^2
            //             = 1 / (1 + 2*(d/r) + (d/r)^2)
            // For more details see: https://imdoingitwrong.wordpress.com/2011/01/31/light-attenuation/
            float kDefaultPointLightRadius = 0.25;
            float atten =         1.0 / (1.0 +   d/kDefaultPointLightRadius);//pow(1.0 +   d/kDefaultPointLightRadius, 1);
            float kCutoff = 1.0 / (1.0 + 1.0/kDefaultPointLightRadius);//pow(1.0 + 1.0/kDefaultPointLightRadius, 1); // cutoff equal to attenuation at distance 1.0

            // Force attenuation to fall towards zero at distance 1.0
            atten = (atten - kCutoff) / (1.f - kCutoff);
            if (d >= 1.f)
                atten = 0.f;
		        
            return atten;
        }
    
        float3 getAreaLightAt(float3 posWS)
        {
            float4 p = TransformWorldToShadow(posWS);
    #if defined(SHADER_API_GLCORE) || defined (SHADER_API_GLES) || defined (SHADER_API_GLES3)
        float z = p.z;
    #else
        float z = 1 - p.z;
    #endif
            float att = 1;
            //{
            att *= lerp(1, saturate(AttenuationToZero(z)), _DistanceFalloffCoe);
            p.xy = p.xy * 2 - 1;
            // Magic tweaks to the shape
            // float corner = 0.4;
            // float outset = 0.8;
            // float smooth = 0.7;
            float corner = _Corner;
            float outset = _Outset;
            float smooth = _Smooth;
            // 倒角
            float d = length(max(abs(p.xy) - 1 + corner*outset, 0.0)) - corner;
            att *= saturate(1 - smoothstep(-smooth, 0, d));
            att *= smoothstep(-0.01, 0.01, z);
            //}
            // 边缘虚化
            att *= lightDirectionFalloff(posWS, 0);

            float3 posToView = normalize(GetCameraPositionWS().xyz - posWS);
            float3 lightPos = float3(0, _LightPosition.y / _LightPosition.w, 0);
            float3 lightPosWS = TransformObjectToWorld(lightPos);
            float3 lightToPos = posWS - lightPosWS;
            float costheta = dot(posToView, normalize(lightToPos));
            att *= phaseHG(costheta, _HGFactor) * _HGPhaseCurve;

            return PI * visibilityAt(posWS, lightPosWS) * att * _LightColor * _LightIntensity;
        }

        float3 inScatteredLightAt(float3 pos)
        {
            return getAreaLightAt(pos);
        }
        
        float3 calcLight(float3 ray, float near, float far, float rand, out float transmittance) // Alpha is total transmittance
        {
            transmittance = 1;
            float3 totalLight = 0;
            float step = min(_Steps, _MAX_STEP);
            float stepSize = (far - near) / step;
            float3 pos;
            float dd = rand * stepSize;
            float len = near;

            [loop]
            for(int i = 0; i < step; i++)
            {
                len += dd;
                pos = GetCameraPositionWS() + ray * len;
                float sigmaE = extinctionAt(pos);
                float3 S = inScatteredLightAt(pos) * sigmaSAt(pos);
                float3 Sint = (S - S * exp(-sigmaE * stepSize)) / sigmaE;
                totalLight += transmittance * Sint;
                transmittance *= exp(-sigmaE * stepSize);
                dd = stepSize;
            }
            return totalLight;
        }
        
        Varyings VertFrustum(Attributes IN)
        {
            Varyings OUT = (Varyings) 0;
            
            OUT.positionWS.xyz = TransformObjectToWorld(IN.positionOS.xyz);
            OUT.positionHCS = TransformWorldToHClip(OUT.positionWS.xyz);
            OUT.positionSS = ComputeScreenPos(OUT.positionHCS);
            return OUT;
        }
        
        FragOutput FragFrustum(Varyings IN) : SV_Target
        {
            FragOutput output;
            
            float2 screenUV = IN.positionSS.xy / IN.positionSS.w;
            float cameraDepth = 0;
            
            #if _VOLUMETRIC_LIGHT_DEPTHTEX_ENABLE
                cameraDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
            #endif
            
            
            // Jitter sample
            float rand = 0;
            //uv偏移  在管线资源里
        #if _VOLUMETRIC_LIGHT_NOISE_ENABLE
            rand = sampleOffset(screenUV);
        #endif 
            float3 ray = normalize(IN.positionWS - GetCameraPositionWS());
            float near, far, depth;
            depth = getBoundary(ray, near, far);
            
            float3 nearPositionWS = GetCameraPositionWS() + ray * near;
            float4 nearPositionCS = TransformWorldToHClip(nearPositionWS);
            //近平面小于相机深度

            
            #if defined(SHADER_API_GLCORE) || defined (SHADER_API_GLES) || defined (SHADER_API_GLES3)
                float nearDepth = nearPositionCS.z / nearPositionCS.w * 0.5 + 0.5;
            #else
                float nearDepth = nearPositionCS.z / nearPositionCS.w;
            #endif
            #if _VOLUMETRIC_LIGHT_DEPTHTEX_ENABLE
                #if UNITY_REVERSED_Z
                    clip(nearDepth - cameraDepth);
                #else
                    clip(cameraDepth - nearDepth);
                #endif
            #endif
            
            //后处理是否需要阴影
        #if _VOLUMETRIC_LIGHT_SHADOW_ENABLE
            //最远的距离
            float3 farPositionWS =  GetCameraPositionWS() + ray * far;
        #endif
            //世界空间深度值
            float depthWS = GetWorldDepth(IN.positionWS, cameraDepth);
            #if _VOLUMETRIC_LIGHT_DEPTHTEX_ENABLE
                far = min(far, depthWS);
            #endif

            // Volumetric ray-marching
            float transmittance = 1;
            float3 color = 0;
            color = calcLight(ray, near, far, rand, transmittance);
            
            float sigmaE = extinctionAt(nearPositionWS);
            transmittance = lerp(1, exp(-sigmaE * near), _OutScatteringLoss);
            color *= transmittance;
            
            output.color = half4(color, Luminance(color));
        #if _VOLUMETRIC_LIGHT_SHADOW_ENABLE
            float2 previousScreenUV = GetObjectPreScreenUV(IN.positionWS, farPositionWS);
            output.motion.xy = screenUV - previousScreenUV;
        #endif
            return output;
        }           

#endif