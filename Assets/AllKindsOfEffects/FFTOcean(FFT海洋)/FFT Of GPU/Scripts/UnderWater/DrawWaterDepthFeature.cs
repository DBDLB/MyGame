using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DrawWaterDepthFeature : ScriptableRendererFeature
{
    public Material material;
    // public RenderTexture WaterDepth;
    public RenderTexture WaterDepthWorldSpace;
    // public Texture screenBrokenNormal;
    public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;
    DrawWaterDepth underwaterPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(underwaterPass);
    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        underwaterPass.Setup(renderer.cameraColorTargetHandle);
    }

    public void CleanRT()
    {
        underwaterPass.ClearRT();
    }

    public override void Create()
    {
        this.name = "Underwater rendering pass";
        underwaterPass = new DrawWaterDepth(Event, material, this.name ,WaterDepthWorldSpace);
    }
}

public class DrawWaterDepth : ScriptableRenderPass
{
    private readonly string tag;
    private Material material;
    private RenderTargetIdentifier source;
    private RenderTargetIdentifier tempBuff;
    private RenderTexture WaterDepthTexture;
    private RenderTexture WaterDepthWorldSpace;
    public DrawWaterDepth(RenderPassEvent renderPassEvent, Material material, string tag,RenderTexture WaterDepthWorldSpace)
    {
        this.renderPassEvent = renderPassEvent;
        this.tag = tag;
        this.material = material;
        this.WaterDepthWorldSpace = WaterDepthWorldSpace;
        // material.SetTexture("_ScreenBrokenNormal", screenBrokenNormal);
    }

    public void ClearRT()
    {

    }

    public void Setup(RenderTargetIdentifier source)
    {
        this.source = source;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // int buffid = Shader.PropertyToID("tempBuff");
        CommandBuffer cmd = CommandBufferPool.Get(tag);
#if UNITY_EDITOR
        if (renderingData.cameraData.isSceneViewCamera) return;
#endif
        // cmd.GetTemporaryRT(buffid, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
        // tempBuff = new RenderTargetIdentifier(buffid);
        // cmd.Blit(source, tempBuff, material);
        
        material.SetTexture("_WaterDepthWorldSpace", WaterDepthWorldSpace);
        cmd.Blit(source, WaterDepthWorldSpace, material,1);
        //保存WaterDepthTexture
        
        
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    public override void FrameCleanup(CommandBuffer cmd)
    {

    }
}