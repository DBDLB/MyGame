using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DrawSandRenderFeature : ScriptableRendererFeature
{
    // public Shader shader;
    // public Texture screenBrokenNormal;
    public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;
    NScreenBrokenPass screenBrokenPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(screenBrokenPass);
    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        screenBrokenPass.Setup(renderer.cameraColorTargetHandle);
    }

    public void CleanRT()
    {
        screenBrokenPass.ClearRT();
    }

    public override void Create()
    {
        this.name = "The Sand PostProcessing rendering pass";
        screenBrokenPass = new NScreenBrokenPass(Event, this.name);
    }
}

public class NScreenBrokenPass : ScriptableRenderPass
{
    private readonly string tag;
    // private Material material;
    private RenderTargetIdentifier source;
    private RenderTargetIdentifier tempBuff;
    public NScreenBrokenPass(RenderPassEvent renderPassEvent, string tag)
    {
        this.renderPassEvent = renderPassEvent;
        this.tag = tag;
        // this.material = material;
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
        cmd.Blit(ComputeTexFlow.tex, source);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {

    }
}