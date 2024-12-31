using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class VolumetricLightFeature : ScriptableRendererFeature
{
    
    //面板设置的参数
    [System.Serializable]
    public class VolumetricLightSetting
    {
        public bool enable = true;
        [Header("贴图缩放")] 
        [Range(0.4f, 1)]
        public float _Scale = 0.6f;
        [Header("步进次数")]
        [Range(1, 16)] 
        public int _StepCount = 4;
        [Header("是否采样深度图")]
        public bool enableDepthTex = true;
        [Header("是否使用阴影")]
        public bool enableShadow = true;
        [Header("是否使用噪声图")]
        public bool enableNoise = true;
        [Header("是否使用模糊")]
        public bool enableBlur = false;
        public Texture2D[] blueNoise64;
        public Texture2D blueNoiseRGBA64;
        [Header("是否使用TAA滤波")]
        public bool enableTime = true;
        [Header("权重值")]
        [Range(0,1)]
        public float feedbackMin = 0.88f;
        [Range(0,1)]
        public float feedbackMax = 0.97f;
        [Header("优化")]
        public bool useYCOCG = true;
        [HideInInspector]
        public bool useClamp = true;
        public bool useClipping = true;
        public bool useOptimizations = true;
        public bool useVarianceClip = true;
        [Range(0,2)]
        public float varianceCoe = 1;
        
        public Shader lightShader;

        public RenderPassEvent _event = RenderPassEvent.AfterRenderingTransparents;

    }
    
    public VolumetricLightSetting _Setting = new VolumetricLightSetting();
    private VolumetricLightPass _volumetricLightPass;
    
    public static void SafeDestroy(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying)
            Destroy(o);
        else
            DestroyImmediate(o);
    }
    private void OnDestroy()
    {
        _volumetricLightPass?.Dispose();
    }
    
    //初始化
    public override void Create()
    {
#if UNITY_EDITOR
        _Setting.blueNoise64 = new Texture2D[64];
        string path = "Assets/AllKindsOfEffects/VolumetricLight（体积光）/Runtime/Textures/BlueNoise/BlueNoise_64/";
        for (int i = 0; i < 64; i++)
        {
            string assetPath = path + "LDR_LLL1_" + i.ToString() + ".png";
            _Setting.blueNoise64[i] =  
                AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }
        if(_Setting.blueNoiseRGBA64 == null) 
            _Setting.blueNoiseRGBA64 = 
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/AllKindsOfEffects/VolumetricLight（体积光）/Runtime/Textures/BlueNoise/LDR_RGBA_0.png");
#endif
        if (_Setting.lightShader == null)
            _Setting.lightShader = Shader.Find("Athena/FrustumMeshVolumetricLightShadow");
        
        //调用构造函数
        _volumetricLightPass = new VolumetricLightPass();
    }
    
    //用于设置，执行渲染。
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!_Setting.enable) return;
        _volumetricLightPass.Setup(_Setting,renderer);
        //渲染进入队列
        renderer.EnqueuePass(_volumetricLightPass);
        
    }
}

public class VolumetricLightPass : ScriptableRenderPass
{
    const string profilerTag = "VolumetricLightPass";
    ProfilingSampler _profilingSampler = new ProfilingSampler(profilerTag);
    RenderTargetHandle _lightRT;

    private Material m_VolumetricLightMat;
    ShaderTagId shaderTag = new ShaderTagId("FrustumVolumetricLight(Shadow)");
    
    public RenderTexture[] _HistoryRT = new RenderTexture[2];
    public RenderTargetIdentifier[] _Mrt = new RenderTargetIdentifier[2];
    
    RenderTexture m_VolumetricObjectMotionRT;
    RenderTexture m_VolumetricLightBlurRT;
    
    //前一帧的VP
    Matrix4x4 m_preVP;

    private Vector4 m_historyBlendParams = new Vector4(0, 0, 1, 0);
    
    VolumetricLightFeature.VolumetricLightSetting _settings;
    
    ScriptableRenderer _renderer;
    
    public int curRtIndex = -1;
    
    public void CreateMaterials(VolumetricLightFeature.VolumetricLightSetting setting)
    {
        if (m_VolumetricLightMat == null)
            m_VolumetricLightMat = new Material(setting.lightShader);
    }
    
    private void DestroyMaterials()
    {
        if (m_VolumetricLightMat != null)
            VolumetricLightFeature.SafeDestroy(m_VolumetricLightMat);
    }
    
        
    public void Dispose()
    {
        DestroyMaterials();
        for (var i = 0; i < _HistoryRT.Length; i++)
        {
            if (_HistoryRT[i] != null)
            {
                RenderTexture.ReleaseTemporary(_HistoryRT[i]);
                _HistoryRT[i] = null;
            }
        }
        RenderTexture.ReleaseTemporary(m_VolumetricObjectMotionRT);
        RenderTexture.ReleaseTemporary(m_VolumetricLightBlurRT);

        curRtIndex = -1;
    }
    
    //传递Renderer Settings 设置event
    public void Setup(VolumetricLightFeature.VolumetricLightSetting setting, ScriptableRenderer renderer)
    {
        _settings = setting;
        _renderer = renderer;
        renderPassEvent = setting._event;
    }

    //新的构造方法
    public VolumetricLightPass()
    {
        _lightRT.Init(m_VolumetricLightRenderTargetName);
    }
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        ConfigureColorTextureDesc(ref cameraTextureDescriptor, RenderTextureFormat.ARGBHalf);
        cmd.GetTemporaryRT(_lightRT.id, cameraTextureDescriptor, FilterMode.Point);

        m_VolumetricObjectMotionRT = GetRenderTexture(m_VolumetricLightMotionName, ref cameraTextureDescriptor,
            FilterMode.Bilinear, m_VolumetricObjectMotionRT, out _);

        _Mrt[0] = _lightRT.id;
        _Mrt[1] = m_VolumetricObjectMotionRT;
        
        ConfigureTarget(_Mrt,_Mrt[0]);
        ConfigureClear(ClearFlag.Color, Color.black);

    }

    public void PrepareVolumetricLightPassData(ScriptableRenderContext context,ref RenderingData renderingData,CommandBuffer cmd)
    {
        // 创建并配置用于体积光照的材质（函数不在提供的代码段中）
        CreateMaterials(_settings);

        // 从renderingData中获取与相机相关的数据
        var cameraData = renderingData.cameraData;
        
        // 创建渲染纹理描述符，基于相机的属性
        RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
        
        // 配置渲染纹理描述符以适应特定的颜色纹理格式
        ConfigureColorTextureDesc(ref descriptor, RenderTextureFormat.ARGBHalf);
        
        // 如果启用了模糊效果
        if (_settings.enableBlur)
            // 获取用于模糊处理的渲染纹理，如果不存在则创建一个
            m_VolumetricLightBlurRT = GetRenderTexture(m_VolumetricLightBlurName, ref descriptor, FilterMode.Bilinear,
                m_VolumetricLightBlurRT, out _);

        // 如果当前渲染目标索引为-1，将其设置为0
        if (curRtIndex == -1)
        {
            curRtIndex = 0;
        }

        // 如果启用了时间相关的效果
        if (_settings.enableTime)
        {
            // 获取用于存储历史渲染图像的渲染纹理，如果不存在则创建一个
            _HistoryRT[curRtIndex] = GetRenderTexture(LightTexNames[curRtIndex], ref descriptor,
            FilterMode.Bilinear, _HistoryRT[curRtIndex], out _);
            
            // 设置历史渲染图像的混合参数
            m_historyBlendParams.z = _settings.varianceCoe;
            m_historyBlendParams.x = _settings.feedbackMin;
            m_historyBlendParams.y = _settings.feedbackMax;
        }

    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;
        if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
        {
            return;
        }
        
#if UNITY_2020_1_OR_NEWER
        CommandBuffer cmd = CommandBufferPool.Get();
#else
        CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
#endif
        
        //准备体积光照渲染所需的各种渲染数据
        PrepareVolumetricLightPassData(context,ref  renderingData,cmd);
        

        //用于性能分析和性能测量的代码块
        using (new ProfilingScope(cmd, _profilingSampler))
        {
            
            DoRayMarching(context, ref renderingData, cmd);
            DoBlurPass(context, ref renderingData, cmd);
            DoTimePass(context, ref renderingData, cmd);

            // if (renderingData.mergeBlitEnable && renderingData.cameraData.camera.CompareTag("MainCamera") && renderingData.cameraData.renderType == CameraRenderType.Base && 
            //     renderingData.cameraData.cameraType == CameraType.Game)
            // if (renderingData.cameraData.camera.CompareTag("MainCamera") && renderingData.cameraData.renderType == CameraRenderType.Base && 
            //     renderingData.cameraData.cameraType == CameraType.Game)
            // {
            //     SetMergeLightPass(context, renderingData,cmd);
            // }
            // else
            // {
            //     DoComposePass(context, renderingData, cmd);
            // }
            DoComposePass(context, renderingData, cmd);

        }
        
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    //光线步进
    public void DoRayMarching(ScriptableRenderContext context, ref RenderingData renderingData,CommandBuffer cmd)
    {
        // 设置全局着色器变量 "_LightPreVP" 为 m_preVP（可能是预先计算的投影矩阵）
        cmd.SetGlobalMatrix("_LightPreVP",m_preVP);
        
        // 设置全局着色器变量 "_VOLUMETRIC_STEP_COUNT" 为 _settings._StepCount（可能是光线步数）
        cmd.SetGlobalInt(_VOLUMETRIC_STEP_COUNT,_settings._StepCount);
        
        // 根据是否启用噪音设置着色器宏
        SetKeyword(cmd,k_VOLUMETRIC_LIGHT_NOISE_ENABLE,_settings.enableNoise);
        // 根据是否启用阴影设置着色器宏
        SetKeyword(cmd,k_VOLUMETRIC_LIGHT_SHADOW_ENABLE,_settings.enableShadow);
        // 根据是否启用深度纹理设置着色器宏
        SetKeyword(cmd,k_VOLUMETRIC_LIGHT_DEPTHTEX_ENABLE,_settings.enableDepthTex);
        
        // 如果启用噪音效果
        if (_settings.enableNoise)
        {
            // 设置全局着色器变量 "_VOLUMETRIC_LIGHT_NOISE_TEXTURE" 为blueNoise64数组中的一个元素
            cmd.SetGlobalTexture(_VOLUMETRIC_LIGHT_NOISE_TEXTURE,
                _settings.blueNoise64[Time.frameCount % _settings.blueNoise64.Length]);
        }
        
        // 创建渲染设置（DrawingSettings）以配置渲染器的绘制行为,shaderTag为FrustumVolumetricLight(Shadow)，只绘制屏幕中有FrustumVolumetricLight(Shadow) pass的物体
        DrawingSettings m_DrawingSettings = CreateDrawingSettings(shaderTag, ref renderingData, SortingCriteria.CommonTransparent);
        // 创建过滤设置（FilteringSettings）以定义渲染的过滤条件
        FilteringSettings m_FilteringSettings = new FilteringSettings(RenderQueueRange.transparent);
        // 执行命令缓冲区中的命令，这些命令用于设置着色器全局变量
        context.ExecuteCommandBuffer(cmd);
        
        // 清空命令缓冲区，以准备接收新的渲染命令
        cmd.Clear();
        
        // 使用绘制设置和过滤设置，绘制场景中的渲染器
        context.DrawRenderers(renderingData.cullResults,ref m_DrawingSettings,ref m_FilteringSettings);
    }


    public void DoBlurPass(ScriptableRenderContext context,ref RenderingData renderingData, CommandBuffer cmd)
    {
        if (_settings.enableBlur)
        {

            cmd.SetGlobalTexture("_MainTex", _lightRT.id);
            cmd.SetGlobalTexture(_BlueNoiseTexUniformTri,_settings.blueNoiseRGBA64);
            cmd.Blit(_lightRT.id, m_VolumetricLightBlurRT, m_VolumetricLightMat,1);
            
        }
    }

    public void DoTimePass(ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer cmd)
    {
        if (_settings.enableTime)
        {
            SetKeyword(cmd, k_VOLUMETRIC_LIGHT_NOISE_ENABLE, _settings.enableNoise);
            SetKeyword(cmd, k_VOLUMETRIC_LIGHT_USE_CLAMP, _settings.useClamp);
            SetKeyword(cmd, k_VOLUMETRIC_LIGHT_USE_YCOCG, _settings.useYCOCG);
            SetKeyword(cmd, k_VOLUMETRIC_LIGHT_USE_CLIP, _settings.useClipping);
            SetKeyword(cmd, k_VOLUMETRIC_LIGHT_USE_OPTIMIZATIONS, _settings.useOptimizations);
            SetKeyword(cmd, k_VOLUMETRIC_LIGHT_USE_VARIANCE_CLIP, _settings.useVarianceClip);
            
            //缺少最后帧判断
            cmd.SetGlobalVector(_VOLUMETRIC_LIGHT_HISTORY_BLEND_PARAMS,m_historyBlendParams);
            //motion缺少
            cmd.SetGlobalTexture(_VOLUMETRIC_LIGHT_MOTION_TEXTURE,m_VolumetricObjectMotionRT); //motionrt
            var historyIndex = (curRtIndex + _HistoryRT.Length - 1) % _HistoryRT.Length;
            cmd.SetGlobalTexture(_VOLUMETRIC_LIGHT_HISTORY_TEXTURE,_HistoryRT[historyIndex]); //历史帧
            if (_settings.enableBlur)
            {
                cmd.SetGlobalTexture(_VOLUMETRIC_LIGHT_TEXTURE,m_VolumetricLightBlurRT); //blurrt
            }
            else
            {
                cmd.SetGlobalTexture(_VOLUMETRIC_LIGHT_TEXTURE,_lightRT.id); //blurrt
            }
            
            cmd.Blit(null,_HistoryRT[curRtIndex],m_VolumetricLightMat,2);
           m_preVP = GL.GetGPUProjectionMatrix(renderingData.cameraData.GetProjectionMatrix(), true) *
                     renderingData.cameraData.camera.worldToCameraMatrix;
            
        }
        
    }
    private static readonly int s_CameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
    private  readonly int tempRT = Shader.PropertyToID("_tempRT");
    public void DoComposePass(ScriptableRenderContext context, RenderingData renderingData, CommandBuffer cmd)
    {
        cmd.GetTemporaryRT(tempRT, renderingData.cameraData.cameraTargetDescriptor);
        
        if (_settings.enableTime)
        {
            cmd.SetGlobalTexture("_VolLight",_HistoryRT[curRtIndex]);
        }
        else if(_settings.enableBlur)
        {
            cmd.SetGlobalTexture("_VolLight",m_VolumetricLightBlurRT);
        }
        else
        {
            cmd.SetGlobalTexture("_VolLight",_lightRT.id);
        }
        cmd.Blit(_renderer.cameraColorTarget,tempRT,m_VolumetricLightMat,3);
        cmd.Blit(tempRT,_renderer.cameraColorTarget);
        curRtIndex = (curRtIndex + 1) % _HistoryRT.Length;
        cmd.ReleaseTemporaryRT(tempRT);
    }

    public void SetMergeLightPass(ScriptableRenderContext context, RenderingData renderingData, CommandBuffer cmd)
    {
        if (_settings.enableTime)
        {
            cmd.SetGlobalTexture("_Merge_VolumetricLightTexture",_HistoryRT[curRtIndex]);
            
        }
        else if(_settings.enableBlur)
        {
            cmd.SetGlobalTexture("_Merge_VolumetricLightTexture",m_VolumetricLightBlurRT);
        }
        else
        {
            cmd.SetGlobalTexture("_Merge_VolumetricLightTexture",_lightRT.id);
        }
        cmd.SetGlobalFloat("_Merge_EnableVolumetricLight",1.0f);
        curRtIndex = (curRtIndex + 1) % _HistoryRT.Length;
    }
    
    
#if UNITY_2020_1_OR_NEWER
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        base.OnCameraCleanup(cmd);
        cmd.ReleaseTemporaryRT(_lightRT.id);
        
        cmd.SetGlobalFloat("_Merge_EnableVolumetricLight", 0.0f);
    }
#else
    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);
        cmd.ReleaseTemporaryRT(_lightRT.id);
        cmd.SetGlobalFloat("_Merge_EnableVolumetricLight", 0.0f);
    }

#endif
    
    public void Release()
    {
        
    }
    
    private void ConfigureColorTextureDesc(ref RenderTextureDescriptor descriptor, RenderTextureFormat textureFormat)
    {
        descriptor.msaaSamples = 1;
        descriptor.depthBufferBits = 0;
        descriptor.width = (int) (descriptor.width  * _settings._Scale);
        descriptor.height = (int) (descriptor.height * _settings._Scale);
        descriptor.colorFormat = textureFormat;
    }
    
    
    private RenderTexture GetRenderTexture(string name, ref RenderTextureDescriptor descriptor, FilterMode filter,
        RenderTexture rt, out bool createNew)
    {
        createNew = false;
        if (rt != null)
        {
            if (rt.width != descriptor.width || rt.height != descriptor.height || rt.filterMode != filter)
            {
                RenderTexture.ReleaseTemporary(rt);
                rt = null;
            }
        }

        if (rt == null)
        {
            rt = RenderTexture.GetTemporary(descriptor);
            rt.filterMode = filter;
            rt.name = name;
            createNew = true;
        }

        return rt;
    }

    public void SetKeyword(CommandBuffer cmd,string key, bool jud)
    {
        if (jud)
        {
            cmd.EnableShaderKeyword(key);
        }
        else
        {
            cmd.DisableShaderKeyword(key);
        }
    }
    public static readonly int _VOLUMETRIC_STEP_COUNT  = Shader.PropertyToID("_Steps");
    public static readonly int _VOLUMETRIC_LIGHT_NOISE_TEXTURE = Shader.PropertyToID("_VolumetricLightNoiseTexture");
    public static readonly int _BlueNoiseTexUniformTri = Shader.PropertyToID("_BlueNoiseTexUniformTri");
    private static readonly string[] LightTexNames = {"_History_Light1_RT", "_History_Light2_RT"};
    public static readonly int _VOLUMETRIC_LIGHT_HISTORY_BLEND_PARAMS =
        Shader.PropertyToID("_VolumetricLightHistoryBlendParams");
    public static readonly int _VOLUMETRIC_LIGHT_MOTION_TEXTURE = Shader.PropertyToID("_VolumetricLightMotionTexture");
    public static readonly int _VOLUMETRIC_LIGHT_HISTORY_TEXTURE = Shader.PropertyToID("_VolumetricLightHistoryTexture");
    public static readonly int _VOLUMETRIC_LIGHT_TEXTURE = Shader.PropertyToID("_VolumetricLightTexture");
    
    public static readonly string k_VOLUMETRIC_LIGHT_NOISE_ENABLE = "_VOLUMETRIC_LIGHT_NOISE_ENABLE";
    public static readonly string k_VOLUMETRIC_LIGHT_USE_CLAMP = "USE_CLAMP";
    public static readonly string k_VOLUMETRIC_LIGHT_USE_YCOCG = "USE_YCOCG";
    public static readonly string k_VOLUMETRIC_LIGHT_USE_CLIP = "USE_CLIPPING";
    public static readonly string k_VOLUMETRIC_LIGHT_USE_OPTIMIZATIONS = "USE_OPTIMIZATIONS";
    public static readonly string k_VOLUMETRIC_LIGHT_USE_VARIANCE_CLIP = "USE_VARIANCE_CLIP";
    public static readonly string k_VOLUMETRIC_LIGHT_SHADOW_ENABLE = "_VOLUMETRIC_LIGHT_SHADOW_ENABLE";
    public static readonly string k_VOLUMETRIC_LIGHT_DEPTHTEX_ENABLE = "_VOLUMETRIC_LIGHT_DEPTHTEX_ENABLE";
    
    public static readonly string m_VolumetricLightRenderTargetName = "VolumetricLightTarget_RT";
    public static readonly string m_VolumetricLightMotionName = "VolumetricLightMotion_RT";
    public static readonly string m_VolumetricLightBlurName = "VolumetricLightBlur_RT";
    
}
