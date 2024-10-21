using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.XR.XRDisplaySubsystem;

public class ColorTintRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;//在后处理前执行我们的颜色校正
        public Shader shader;//汇入shader
    }
    public Settings settings = new Settings();//开放设置
    ColorTintPass colorTintPass;//设置渲染pass
    public override void Create()//新建pass
    {
        this.name = "ColorTintPass";//名字
        colorTintPass = new ColorTintPass(settings.renderPassEvent, settings.shader);//初始化
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)//Pass逻辑
    {
        renderer.EnqueuePass(colorTintPass);//汇入队列
    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        colorTintPass.ConfigureInput(ScriptableRenderPassInput.Color);
        colorTintPass.Setup(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        colorTintPass.Dispose();
    }
}

//【执行pass】
public class ColorTintPass : ScriptableRenderPass
{
    static readonly string k_RenderTag = "ColorTint Effects";//设置tags
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");//设置主贴图

    ColorTint colorTint;//提供一个Volume传递位置
    Material colorTintMaterial;//后处理使用材质
    RTHandle currentTarget;//设置当前渲染目标
    RTHandle cameraColorAttachment;//设置当前渲染目标
    private RenderTextureDescriptor cameraColorAttachmentDescriptor;
    #region 设置渲染事件
    public ColorTintPass(RenderPassEvent evt,Shader ColorTintShader)
    {
        renderPassEvent = evt;//设置渲染事件位置
        var shader = ColorTintShader;//汇入shader
        //不存在则返回
        if (shader == null)
        {
            Debug.LogError("不存在ColorTint shader");
            return;
        }
        colorTintMaterial = CoreUtils.CreateEngineMaterial(ColorTintShader);//新建材质
        cameraColorAttachmentDescriptor = new RenderTextureDescriptor();

    }
    #endregion

    #region 初始化
    public void Setup(in RTHandle colorHandle)
    {
        this.currentTarget = colorHandle;
       
    }
    #endregion

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        cameraColorAttachmentDescriptor.width = cameraTextureDescriptor.width;
        cameraColorAttachmentDescriptor.height = cameraTextureDescriptor.height;
        cameraColorAttachmentDescriptor.colorFormat = cameraTextureDescriptor.colorFormat;
        cameraColorAttachmentDescriptor.dimension = cameraTextureDescriptor.dimension;
        cameraColorAttachmentDescriptor.msaaSamples = cameraTextureDescriptor.msaaSamples;

        RenderingUtils.ReAllocateIfNeeded(ref cameraColorAttachment, cameraColorAttachmentDescriptor);
    }

    #region 执行
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (colorTintMaterial == null)//材质是否存在
        {
            Debug.LogError("材质初始化失败");
            return;
        }
        //摄像机关闭后处理
        if (!renderingData.cameraData.postProcessEnabled)
        {
            return;
        }
        //【渲染设置】
        var stack = VolumeManager.instance.stack;//传入volume数据
        colorTint = stack.GetComponent<ColorTint>();//拿到我们的Volume
        if (colorTint == null)
        {
            Debug.LogError("Volume组件获取失败");
            return;
        }

        var cmd = CommandBufferPool.Get(k_RenderTag);//设置抬头
        Render(cmd, ref renderingData);//设置渲染函数
        context.ExecuteCommandBuffer(cmd);//执行函数
        CommandBufferPool.Release(cmd);//释放
    }
    #endregion

    #region 渲染
    void Render(CommandBuffer cmd,ref RenderingData renderingData)
    {
        ref var cameraData = ref renderingData.cameraData;//汇入摄像机数据
        var camera = cameraData.camera;//传入摄像机数据
        var source = currentTarget;//当前渲染图片汇入

        colorTintMaterial.SetColor("_ColorTint", colorTint.colorChange.value);//汇入颜色校正
        colorTintMaterial.SetFloat("_startRange", colorTint._startRange.value);
        colorTintMaterial.SetFloat("_endRange", colorTint._endRange.value);
        colorTintMaterial.SetFloat("_scanLineInterval", colorTint._scanLineInterval.value);
        colorTintMaterial.SetFloat("_scanLineWidth", colorTint._scanLineWidth.value);
        colorTintMaterial.SetFloat("_scanLineBrightness", colorTint._scanLineBrightness.value);
        colorTintMaterial.SetFloat("_centerFadeout", colorTint._centerFadeout.value);
        colorTintMaterial.SetVector("_scanCenter", colorTint._scanCenter.value);
        colorTintMaterial.SetVector("_SceneJitterDirection", colorTint._SceneJitterDirection.value);
        colorTintMaterial.SetFloat("_sceneJitterStrength", colorTint._sceneJitterStrength.value);
        colorTintMaterial.SetFloat("_flickerStrength", colorTint._flickerStrength.value);
        colorTintMaterial.SetFloat("_useSceneJitter", colorTint._useSceneJitter.value);
        colorTintMaterial.SetFloat("_noiseStrength", colorTint._noiseStrength.value);
        
        Blitter.BlitCameraTexture(cmd, source, cameraColorAttachment, colorTintMaterial, 0);
        Blitter.BlitCameraTexture(cmd, cameraColorAttachment, source);
    }
    #endregion

    public void Dispose()
    {
        if( cameraColorAttachment!= null)
        {
            cameraColorAttachment.Release();
        }
    }
}
