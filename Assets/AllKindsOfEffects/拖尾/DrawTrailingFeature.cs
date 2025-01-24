using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.XR.XRDisplaySubsystem;

[System.Serializable]
public class DrawTrailingSettings
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;//在后处理前执行我们的颜色校正
    public Shader shader;//汇入shader
    public RenderTexture DrawTexture;
    public RenderTexture perTexture;
}
public class DrawTrailingFeature : ScriptableRendererFeature
{

    public static List<InputMain> playerDatas = new List<InputMain>();
    public List<InputMain> test = new List<InputMain>();
    public DrawTrailingSettings settings = new DrawTrailingSettings();//开放设置
    DrawTrailingFeaturePass colorTintPass;//设置渲染pass
    
    // public ColorTintSettings colorTintSettings = new ColorTintSettings();
    
    public override void Create()//新建pass
    {
        this.name = "DrawTrailingFeaturePass";//名字
        colorTintPass = new DrawTrailingFeaturePass(settings, settings.shader);//初始化
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)//Pass逻辑
    {
        if (playerDatas.Count == 0)
        {
            return;
        }
        renderer.EnqueuePass(colorTintPass);//汇入队列
    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        test = playerDatas;
        // colorTintPass.ConfigureInput(ScriptableRenderPassInput.Color);
        // colorTintPass.Setup(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        colorTintPass.Dispose();
    }
}

//【执行pass】
public class DrawTrailingFeaturePass : ScriptableRenderPass
{
    static readonly string k_RenderTag = "DrawTrailing";//设置tags
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");//设置主贴图

    // ColorTint colorTint;//提供一个Volume传递位置
    Material colorTintMaterial;//后处理使用材质
    RTHandle currentTarget;//设置当前渲染目标
    RTHandle cameraColorAttachment;//设置当前渲染目标
    private RenderTextureDescriptor cameraColorAttachmentDescriptor;
    DrawTrailingSettings settings;
    #region 设置渲染事件
    public DrawTrailingFeaturePass(DrawTrailingSettings settings,Shader ColorTintShader)
    {
        this.settings = settings;//设置
        renderPassEvent = settings.renderPassEvent;//设置渲染事件位置
        var shader = ColorTintShader;//汇入shader
        //不存在则返回
        if (shader == null)
        {
            Debug.LogError("不存在ColorTint shader");
            return;
        }
        colorTintMaterial = CoreUtils.CreateEngineMaterial(ColorTintShader);//新建材质

    }
    #endregion

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
    }

    #region 执行
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (colorTintMaterial == null)//材质是否存在
        {
            Debug.LogError("材质初始化失败");
            return;
        }
        //【渲染设置】
        // var stack = VolumeManager.instance.stack;//传入volume数据
        // colorTint = stack.GetComponent<ColorTint>();//拿到我们的Volume
        // if (colorTint == null)
        // {
        //     Debug.LogError("Volume组件获取失败");
        //     return;
        // }

        var cmd = CommandBufferPool.Get(k_RenderTag);//设置抬头
        Render(cmd, ref renderingData);//设置渲染函数
        context.ExecuteCommandBuffer(cmd);//执行函数
        CommandBufferPool.Release(cmd);//释放
    }
    #endregion

    #region 渲染        
    Vector4[] uvOffset = new Vector4[5];

    void Render(CommandBuffer cmd,ref RenderingData renderingData)
    {
        ref var cameraData = ref renderingData.cameraData;//汇入摄像机数据
        var camera = cameraData.camera;//传入摄像机数据
        var source = currentTarget;//当前渲染图片汇入

       

        int ArrayCount = Mathf.Min(DrawTrailingFeature.playerDatas.Count, 5);
        for (int i = 0; i < ArrayCount; i++)
        {
            var data = DrawTrailingFeature.playerDatas[i];
            data.pdata.currentPosition = data.transform.position;
            bool playerIS = DrawTrailingFeature.playerDatas[i].pdata.isPlayer;

            if (playerIS)
            {
                data.pdata.uvOffset = data.pdata.currentPosition - data.pdata.lastPosition;
                data.pdata.lastPosition =  data.pdata.currentPosition;
            }
            else
            {
                data.pdata.uvOffset = data.pdata.currentPosition;
            }
          
            if(DrawTrailingFeature.playerDatas[i] != null)
            {
                uvOffset[i] = data.pdata.uvOffset;
            }

        }
        
        colorTintMaterial.SetInt("ArrayCount",ArrayCount);
        colorTintMaterial.SetVectorArray("testPostion", uvOffset);//传入位置
        colorTintMaterial.SetVector("PlayerPostion", DrawTrailingFeature.playerDatas[0].pdata.currentPosition);//传入位置
        cmd.Blit(settings.perTexture, settings.DrawTexture, colorTintMaterial, 0);//传入图片
        cmd.Blit(settings.DrawTexture,  settings.perTexture, colorTintMaterial, 1);//传入图片
        // Blitter.BlitCameraTexture(cmd, source, settings.DrawTexture, colorTintMaterial, 0);
    }
    #endregion

    public void Dispose()
    {
        // if( cameraColorAttachment!= null)
        // {
        //     cameraColorAttachment.Release();
        // }
    }
}
