using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UnderwaterFeature : ScriptableRendererFeature
{
    public Material material;
    public RenderTexture rt;
    // public Texture screenBrokenNormal;
    public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;
    UnderwaterPass underwaterPass;
    
    UnderwaterVolume underwater_volume;


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var stac = VolumeManager.instance.stack;
        if (stac.GetComponent<UnderwaterVolume>() != null)
        {
            underwater_volume = stac.GetComponent<UnderwaterVolume>();
        }
        
        if (underwater_volume == null || !underwater_volume.IsActive())
        {
            return;
        }
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
        underwaterPass = new UnderwaterPass(Event, material, this.name,rt);
    }
}

public class UnderwaterPass : ScriptableRenderPass
{
    private readonly string tag;
    private Material material;
    private RenderTargetIdentifier source;
    private RenderTargetIdentifier tempBuff;
    private RenderTexture WaterDepthWorldSpace;
    
    public UnderwaterPass(RenderPassEvent renderPassEvent, Material material, string tag,RenderTexture rt)
    {
        this.renderPassEvent = renderPassEvent;
        this.tag = tag;
        this.material = material;
        this.WaterDepthWorldSpace = rt;
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
        int buffid = Shader.PropertyToID("tempBuff");
        CommandBuffer cmd = CommandBufferPool.Get(tag);
#if UNITY_EDITOR
        if (renderingData.cameraData.isSceneViewCamera) return;
#endif
        cmd.GetTemporaryRT(buffid, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
        tempBuff = new RenderTargetIdentifier(buffid);
        // cmd.Blit(source, tempBuff, material);
        
        material.SetTexture("_WaterDepthWorldSpace", WaterDepthWorldSpace);
        cmd.Blit(source, tempBuff, material,0);
        cmd.Blit(tempBuff, source);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {

    }
}