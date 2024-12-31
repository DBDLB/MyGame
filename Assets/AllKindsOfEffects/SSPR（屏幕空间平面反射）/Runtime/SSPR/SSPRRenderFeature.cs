using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSPRRenderFeature : ScriptableRendererFeature
{
    public enum RTSize
    {
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024
    }
    
    public SSRPSetting setting;
    [Serializable]
    public class SSRPSetting
    {
        public ComputeShader ssprCS;
        public RTSize rtSize = RTSize._128;
        public RenderPassEvent renderEvent = RenderPassEvent.BeforeRenderingTransparents;

        public void Init()
        {
            #if UNITY_EDITOR
                if (ssprCS == null)
                {
                    ssprCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                        "Packages/com.pwrd.athena-framework.renderer/Extensions/Runtime/SSPR/SSPR.compute");
                }
            #endif
        }
    }

    [Header("垂直高度")][Range(0.1f, 100f)]public float ReflectionPlaneHeightWS = 0.1f;

    [Header("屏幕淡出-垂直范围")][Range(0.01f, 1f)]
    public float FadeOutVerticle = 0.25f;

    [Header("屏幕淡出-水平范围")][Range(0.01f, 1f)]
    public float FadeOutHorizontal = 0.35f;

    [Header("屏幕边缘-水平偏移")][Range(0.01f, 1f)]
    public float _ScreenLRStretchThreshold = 0.7f;
    public float _ScreenLRStretchIntensity = 0.4f;

    [Header("模糊半径")][Range(0f, 10f)] 
    public float BlurRadius = 0;
    
    private string featureName = "SSPRRenderFeature";
    private SSPRRenderPass _ssprRenderPass;
    private SSPRSetPass _ssprSetPass;
    private UniversalRenderPipelineAsset cacheCurrentRenderPipeline;
    private Dictionary<CameraType, SSPRCameraTextureData> _cameraDataDic = new Dictionary<CameraType, SSPRCameraTextureData>();
    
    public override void Create()
    {
        this.name = featureName;
        if (setting == null)
        {
            setting = new SSRPSetting();
            setting.Init();
        }
        _ssprRenderPass = new SSPRRenderPass();
        _ssprSetPass = new SSPRSetPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (setting.ssprCS == null) return;
        var camera = renderingData.cameraData.camera;

        if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView) return;
        if (camera.cameraType == CameraType.Game && camera.CompareTag("MainCamera") == false) //game视图目前只支持主相机
            return;
        
        // if (cacheCurrentRenderPipeline == null)
        // {
        //     cacheCurrentRenderPipeline = (UniversalRenderPipelineAsset) GraphicsSettings.currentRenderPipeline;
        // }
        // if (cacheCurrentRenderPipeline)
        // {
        //     cacheCurrentRenderPipeline.ForceEnableRequireOpaqueTexture = true; //防止其他需要opaque图的feature释放，所以实时修改
        // }

        var ssprCameraTextureData = SetupCameraData(ref renderingData);
        
        _ssprRenderPass.SetUp(setting, ssprCameraTextureData, ReflectionPlaneHeightWS, FadeOutVerticle, FadeOutHorizontal, _ScreenLRStretchThreshold, _ScreenLRStretchIntensity, BlurRadius);
        renderer.EnqueuePass(_ssprRenderPass);
        _ssprSetPass.Setup((ssprCameraTextureData.SSPRCameraTexture, ssprCameraTextureData.preVP));
        renderer.EnqueuePass(_ssprSetPass);
    }

    protected override void Dispose(bool disposing)
    {
        if (_cameraDataDic != null)
        {
            foreach (var ssprCameraTextureData in _cameraDataDic.Values)
            {
                ssprCameraTextureData?.Clear();
            }
            _cameraDataDic.Clear();
        }

        _ssprRenderPass.Dispose();
        _ssprSetPass.Dispose();
        
        // if (cacheCurrentRenderPipeline)
        // {
        //     cacheCurrentRenderPipeline.ForceEnableRequireOpaqueTexture = false;
        // }
    }

    private SSPRCameraTextureData SetupCameraData(ref RenderingData renderingData)
    {
        // 相机加入字典
        var camera = renderingData.cameraData.camera;
        if (!_cameraDataDic.TryGetValue(camera.cameraType, out var cameraData))
        {
            cameraData = new SSPRCameraTextureData();
            _cameraDataDic.Add(camera.cameraType, cameraData);
        }
        
        //更新纹理
        var cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        var rtDescriptor = cameraTextureDescriptor;//new RenderTextureDescriptor((int) rtSize.x, (int) rtSize.y);
        rtDescriptor.msaaSamples = 1;
        rtDescriptor.mipCount = 0;
        rtDescriptor.depthBufferBits = 0;
        rtDescriptor.sRGB = false;
        rtDescriptor.enableRandomWrite = true;
        rtDescriptor.dimension = TextureDimension.Tex2D;
        
        var rtSizeCurrent = new Vector2Int(GetRTWidth(), GetRTHeight());
        rtDescriptor.width = (int) rtSizeCurrent.x;
        rtDescriptor.height = (int) rtSizeCurrent.y;
        
        cameraData.rtSize = rtSizeCurrent;
        cameraData.dispatchThreadGroupXCount = (int)rtSizeCurrent.x / SHADER_NUMTHREAD_X;
        cameraData.dispatchThreadGroupYCount = (int)rtSizeCurrent.y / SHADER_NUMTHREAD_Y;
        cameraData.SHADER_NUMTHREAD_X = SHADER_NUMTHREAD_X;
        cameraData.SHADER_NUMTHREAD_Y = SHADER_NUMTHREAD_Y;
        
        //SSPRCameraTexture
        rtDescriptor.colorFormat = RenderTextureFormat.ARGB32;
        rtDescriptor.mipCount = 7;//更高的层数，对于反射内容没有实际意义
        rtDescriptor.useMipMap = true;
        cameraData.SSPRCameraTexture =
            GetTexture(cameraData.SSPRCameraTexture, "SSPRCameraTexture", rtDescriptor, true);
        
        //SSPRPositionWSyTexture
        rtDescriptor.colorFormat = GetSafeTextureFormat();
        rtDescriptor.mipCount = 0;
        rtDescriptor.useMipMap = false;
        cameraData.SSPRPositionWSyTexture = 
            GetTexture(cameraData.SSPRPositionWSyTexture, "SSPRPositionWSyTexture", rtDescriptor, false);
        
        return cameraData;
    }
    
    RenderTextureFormat GetSafeTextureFormat()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
        {
            return RenderTextureFormat.RFloat;
        }
        else
        {
            return RenderTextureFormat.ARGBFloat; 
        }
#endif
        return RenderTextureFormat.RInt;
        
    }
    
    private RenderTexture GetTexture(RenderTexture renderTexture, string name, RenderTextureDescriptor renderTextureDescriptor, bool useTrilinear = false)
    {
        if (renderTexture == null)
        {
            renderTexture = CreateRenderTexture(name, renderTextureDescriptor, useTrilinear);
        }
        else
        {
            if (renderTexture.width != renderTextureDescriptor.width || renderTexture.height != renderTextureDescriptor.height)
            {
                renderTexture.Release();
                renderTexture = CreateRenderTexture(name, renderTextureDescriptor, useTrilinear);
            }
        }

        return renderTexture;
    }
    
    private RenderTexture CreateRenderTexture(string name, RenderTextureDescriptor renderTextureDescriptor, bool useTrilinear = false)
    {
        var reflectTex = new RenderTexture(renderTextureDescriptor);
        reflectTex.name = name;
        reflectTex.enableRandomWrite = true;
        if (useTrilinear) reflectTex.filterMode = FilterMode.Trilinear;
        reflectTex.Create();
        return reflectTex;
    }
    
    const int SHADER_NUMTHREAD_X = 8; //must match compute shader's [numthread(x)]
    const int SHADER_NUMTHREAD_Y = 8; //must match compute shader's [numthread(y)]
    int GetRTHeight()
    {
        return Mathf.CeilToInt((float)setting.rtSize / SHADER_NUMTHREAD_Y) * SHADER_NUMTHREAD_Y;
    }
    
    int GetRTWidth()
    {
        float aspect = (float)Screen.width / Screen.height;
        return Mathf.CeilToInt(GetRTHeight() * aspect / (float)SHADER_NUMTHREAD_X) * SHADER_NUMTHREAD_X;
    }
    
    //管理多个相机RT显示，防止竞争
    public class SSPRCameraTextureData
    {
        public RenderTexture SSPRCameraTexture;
        public RenderTexture SSPRPositionWSyTexture;
        public Vector2Int rtSize;
        public Matrix4x4 preVP;
        public int dispatchThreadGroupXCount;
        public int dispatchThreadGroupYCount;
        public int SHADER_NUMTHREAD_X;
        public int SHADER_NUMTHREAD_Y;

        public void Clear()
        {
            if (SSPRCameraTexture != null)
            {
                SSPRCameraTexture.Release();
                CoreUtils.Destroy(SSPRCameraTexture);
                SSPRCameraTexture = null;
            }
            if (SSPRPositionWSyTexture != null)
            {
                SSPRPositionWSyTexture.Release();
                CoreUtils.Destroy(SSPRPositionWSyTexture);
                SSPRPositionWSyTexture = null;
            }
        }
    }
}

//Opaque实际使用的是上一帧的RT，所以每帧根据相机设回，_SSPRCameraTexture中（不release）因为GetTemporaryRT每帧回池后再取出顺序不一致
internal class SSPRSetPass : ScriptableRenderPass
{
    private RenderTexture rt;
    private Matrix4x4 preVP;

    public void Setup((RenderTexture, Matrix4x4) data)
    {
        renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        this.rt = data.Item1;
        this.preVP = data.Item2;
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        cmd.name = "SSPR Set";
        
        cmd.SetGlobalTexture("_SSPRCameraTexture", rt);
        cmd.SetGlobalMatrix("unity_MatrixPreviousVP",preVP);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    
    public void Dispose()
    {
        
    }
}

internal class SSPRRenderPass : ScriptableRenderPass
{
    #region propertyID
    static readonly int _SSPRCameraTexture = Shader.PropertyToID("_SSPRCameraTexture");
    static readonly int _SSRPMipSource = Shader.PropertyToID("_SSRPMipSource");
    static readonly int _SSRPMipDest = Shader.PropertyToID("_SSRPMipDest");
    //static readonly int _SSPRPackedDataTexture = Shader.PropertyToID("_SSPRPackedDataTexture");
    static readonly int _SSPRPositionWSyTexture = Shader.PropertyToID("_SSPRPositionWSyTexture");
    static readonly int _CameraOpaqueTexture = Shader.PropertyToID("_CameraOpaqueTexture");
    static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        
    static readonly int _RTSize = Shader.PropertyToID("_RTSize");
    static readonly int _HashRT = Shader.PropertyToID("_HashRT");
    static readonly int _ReflectionPlaneHeightWS = Shader.PropertyToID("_ReflectionPlaneHeightWS");
    static readonly int _FadeOutVerticle = Shader.PropertyToID("_FadeOutVerticle");
    static readonly int _FadeOutHorizontal = Shader.PropertyToID("_FadeOutHorizontal");
    static readonly int _CameraDirection = Shader.PropertyToID("_CameraDirection");
    static readonly int _MATRIX_VP = Shader.PropertyToID("MATRIX_VP");
    static readonly int _MATRIX_I_VP = Shader.PropertyToID("MATRIX_I_VP");
    static readonly int _ScreenLRStretchThreshold = Shader.PropertyToID("_ScreenLRStretchThreshold");
    static readonly int _ScreenLRStretchIntensity = Shader.PropertyToID("_ScreenLRStretchIntensity");
    #endregion

    RenderTargetIdentifier CameraOpaqueTexture = new RenderTargetIdentifier(_CameraOpaqueTexture);
    RenderTargetIdentifier CameraDepthTexture = new RenderTargetIdentifier(_CameraDepthTexture);
    //private Matrix4x4 vp;

    private ComputeShader ssprCS;
    private SSPRRenderFeature.SSRPSetting setting;
    private SSPRRenderFeature.SSPRCameraTextureData ssprCameraTextureData;
    
    private float my_ReflectionPlaneHeightWS;
    private float my_FadeOutVerticle;
    private float my_FadeOutHorizontal;
    private float my_ScreenLRStretchThreshold;
    private float my_ScreenLRStretchIntensity;
    private float BlurRadius;

    public SSPRRenderPass()
    {
        
    }

    public void SetUp(SSPRRenderFeature.SSRPSetting setting, SSPRRenderFeature.SSPRCameraTextureData ssprCameraTextureData, float ReflectionPlaneHeightWS, float FadeOutVerticle, float FadeOutHorizontal, float ScreenLRStretchThreshold, float ScreenLRStretchIntensity, float BlurRadius)
    {
        this.setting = setting;
        this.renderPassEvent = setting.renderEvent;
        this.ssprCS = setting.ssprCS;
        this.ssprCameraTextureData = ssprCameraTextureData;
        this.my_ReflectionPlaneHeightWS = ReflectionPlaneHeightWS;
        this.my_FadeOutVerticle = FadeOutVerticle;
        this.my_FadeOutHorizontal = FadeOutHorizontal;
        this.my_ScreenLRStretchThreshold = ScreenLRStretchThreshold;
        this.my_ScreenLRStretchIntensity = ScreenLRStretchIntensity;
        this.BlurRadius = BlurRadius;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {

    }
    
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        CameraOpaqueTexture = renderingData.cameraData.renderer.cameraColorTarget;
        CameraDepthTexture = renderingData.cameraData.renderer.cameraDepthTarget;
        // CameraOpaqueTexture = Shader.GetGlobalTexture("_CameraOpaqueTexture");
        // CameraDepthTexture = Shader.GetGlobalTexture("_CameraDepthTexture");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;
        var vp = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;
        //每相机记录绘制时vp，用于设置后采样上一帧使用
        ssprCameraTextureData.preVP = vp;
        var SSPRCameraTexture = ssprCameraTextureData.SSPRCameraTexture;
       
        
        CommandBuffer cmd = CommandBufferPool.Get();
        cmd.name = "SSPR";
        int dispatchThreadGroupXCount = ssprCameraTextureData.dispatchThreadGroupXCount;
        int dispatchThreadGroupYCount = ssprCameraTextureData.dispatchThreadGroupYCount; 
        int dispatchThreadGroupZCount = 1; 
          
        cmd.SetComputeVectorParam(ssprCS, _RTSize, new Vector4(ssprCameraTextureData.rtSize.x, ssprCameraTextureData.rtSize.y, 0,0));
        cmd.SetComputeFloatParam(ssprCS, _ReflectionPlaneHeightWS, my_ReflectionPlaneHeightWS);

        cmd.SetComputeFloatParam(ssprCS,_FadeOutVerticle, my_FadeOutVerticle);
        cmd.SetComputeFloatParam(ssprCS, _FadeOutHorizontal, my_FadeOutHorizontal);
        cmd.SetComputeFloatParam(ssprCS, _ScreenLRStretchThreshold, my_ScreenLRStretchThreshold);
        cmd.SetComputeFloatParam(ssprCS, _ScreenLRStretchIntensity, my_ScreenLRStretchIntensity);
        cmd.SetComputeVectorParam(ssprCS, _CameraDirection, renderingData.cameraData.camera.transform.forward);

        
        Matrix4x4 vpInv = Matrix4x4.Inverse(ssprCameraTextureData.preVP);
        
        cmd.SetComputeMatrixParam(ssprCS, _MATRIX_VP, ssprCameraTextureData.preVP);
        cmd.SetComputeMatrixParam(ssprCS, _MATRIX_I_VP, vpInv);
      
            if (ssprCameraTextureData.SSPRPositionWSyTexture.format != RenderTextureFormat.RInt)
            {
                //Android GLES / Metal
                var SSPRPositionWSyTexture = ssprCameraTextureData.SSPRPositionWSyTexture;

                //kernel MobilePathsinglePassColorRTDirectResolve
                int kernel_ColorRTDirectResolve = ssprCS.FindKernel("MobilePathSinglePassColorRTDirectResolve");
                cmd.SetComputeTextureParam(ssprCS, kernel_ColorRTDirectResolve, _SSPRCameraTexture, SSPRCameraTexture);
                cmd.SetComputeTextureParam(ssprCS, kernel_ColorRTDirectResolve, _SSPRPositionWSyTexture, SSPRPositionWSyTexture);
                cmd.SetComputeTextureParam(ssprCS, kernel_ColorRTDirectResolve, _CameraOpaqueTexture, CameraOpaqueTexture);
                cmd.SetComputeTextureParam(ssprCS, kernel_ColorRTDirectResolve, _CameraDepthTexture, CameraDepthTexture);
                cmd.DispatchCompute(ssprCS, kernel_ColorRTDirectResolve, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);

            }
            else
            {
                //PC/console
                var SSPRPackedDataTexture = ssprCameraTextureData.SSPRPositionWSyTexture;
                // var SSPRPackedDataTexture = ssprCameraTextureData.SSPRPackedDataTexture;

                //kernel NonMobilePathClear
                int kernel_Clear = ssprCS.FindKernel("PCPathClear");
                cmd.SetComputeTextureParam(ssprCS, kernel_Clear, _HashRT, SSPRPackedDataTexture);
                cmd.SetComputeTextureParam(ssprCS, kernel_Clear, "_SSPRCameraTexture", SSPRCameraTexture);
                cmd.DispatchCompute(ssprCS, kernel_Clear, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);

                //kernel NonMobilePathRenderHashRT
                int kernel_RenderHashRT = ssprCS.FindKernel("PCPathRenderHashRT");
                cmd.SetComputeTextureParam(ssprCS, kernel_RenderHashRT, _HashRT, SSPRPackedDataTexture);
                cmd.SetComputeTextureParam(ssprCS, kernel_RenderHashRT, _CameraDepthTexture, CameraDepthTexture);

                cmd.DispatchCompute(ssprCS, kernel_RenderHashRT, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);

                //resolve to ColorRT
                int kernel_ResolveColorRT = ssprCS.FindKernel("PCPathResolveColorRT");
                cmd.SetComputeTextureParam(ssprCS, kernel_ResolveColorRT, _CameraOpaqueTexture, CameraOpaqueTexture);
                cmd.SetComputeTextureParam(ssprCS, kernel_ResolveColorRT, _SSPRCameraTexture, SSPRCameraTexture);
                cmd.SetComputeTextureParam(ssprCS, kernel_ResolveColorRT, _HashRT, SSPRPackedDataTexture);
                cmd.DispatchCompute(ssprCS, kernel_ResolveColorRT, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);
            }

            int kernel_FillHoles = ssprCS.FindKernel("FillHoles");
            cmd.SetComputeTextureParam(ssprCS, kernel_FillHoles, _SSPRCameraTexture, SSPRCameraTexture);
            cmd.DispatchCompute(ssprCS, kernel_FillHoles, Mathf.CeilToInt(dispatchThreadGroupXCount / 2f), Mathf.CeilToInt(dispatchThreadGroupYCount / 2f), dispatchThreadGroupZCount);
            
            //rgb是反射颜色，a是反射使用0~1 mask
            //cmd.SetGlobalTexture(_SSPRCameraTexture, SSPRCameraTexture);
            //SSPRCameraTexture.GenerateMips();//TODO：每帧生成mipmaps是否妥当?


            //gaussian blur && pc
            if (Mathf.CeilToInt(BlurRadius) > 0 && ssprCameraTextureData.SSPRPositionWSyTexture.format == RenderTextureFormat.RInt)
            {
                cmd.SetComputeFloatParam(ssprCS, "_BlurRadius", BlurRadius);

                int kernel_GaussianBlurHorizontal = ssprCS.FindKernel("GaussianBlurHorizontal");
                cmd.SetComputeTextureParam(ssprCS, kernel_GaussianBlurHorizontal, _CameraDepthTexture, CameraDepthTexture);
                cmd.SetComputeTextureParam(ssprCS, kernel_GaussianBlurHorizontal, _SSPRCameraTexture,
                    SSPRCameraTexture);
                cmd.DispatchCompute(ssprCS, kernel_GaussianBlurHorizontal, dispatchThreadGroupXCount,
                    dispatchThreadGroupYCount, dispatchThreadGroupZCount);

                int kernel_GaussianBlurVertical = ssprCS.FindKernel("GaussianBlurVertical");
                cmd.SetComputeTextureParam(ssprCS, kernel_GaussianBlurVertical, _CameraDepthTexture, CameraDepthTexture);
                cmd.SetComputeTextureParam(ssprCS, kernel_GaussianBlurVertical, _SSPRCameraTexture,
                    SSPRCameraTexture);
                cmd.DispatchCompute(ssprCS, kernel_GaussianBlurVertical, dispatchThreadGroupXCount,
                    dispatchThreadGroupYCount, dispatchThreadGroupZCount);
            }

            //mips
            int width = ssprCameraTextureData.rtSize.x;
            int height = ssprCameraTextureData.rtSize.y;
            var count = SSPRCameraTexture.mipmapCount;
            int actualCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Log(Mathf.Max(width, height), 2))); //过低的分辨率降低mip层数
            actualCount = Mathf.Min(actualCount, count);

            int kernel_generateMips = ssprCS.FindKernel("GenerateMips");
            for (int i = 1; i < actualCount; i++)
            {
                width = Mathf.Max(1, width / 2);
                height = Mathf.Max(1, height / 2);
                cmd.SetComputeTextureParam(ssprCS, kernel_generateMips, _SSRPMipSource, SSPRCameraTexture, i - 1);
                cmd.SetComputeTextureParam(ssprCS, kernel_generateMips, _SSRPMipDest, SSPRCameraTexture, i);
                cmd.SetComputeVectorParam(ssprCS, _RTSize, new Vector4(width, height, 0, 0));

                int x, y;
                x = Mathf.Max(1, Mathf.CeilToInt((int)width / ssprCameraTextureData.SHADER_NUMTHREAD_X));
                y = Mathf.Max(1, Mathf.CeilToInt((int)height / ssprCameraTextureData.SHADER_NUMTHREAD_Y));
                cmd.DispatchCompute(ssprCS, kernel_generateMips, x, y, 1);
            }


            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
    }
    
    public override void FrameCleanup(CommandBuffer cmd)
    {
        
    }

    public void Dispose()
    {
        
    }
}
