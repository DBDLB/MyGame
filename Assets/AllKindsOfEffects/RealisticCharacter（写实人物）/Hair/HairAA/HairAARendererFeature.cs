using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

public class HairAARendererFeature : ScriptableRendererFeature
{
    HairAARendererPass hairAARendererPass;
    public Material hairAARenderer_SourceMat;
    public Material hairAARendererMat;

    public GraphicsFormat gfxFormat;


    public RenderPassEvent Event = RenderPassEvent.BeforeRenderingPostProcessing;


    // public LayerMask layerMask;
    public class HairAARendererPass : ScriptableRenderPass
    {

        // SS_OutlineVolume outline_volume;
        public HairAARendererFeature feature;
        private RTHandle cameraColor;
        private RTHandle cameraDepth;
        private readonly RenderTargetHandle HairAATangentRT = RenderTargetHandle.CameraTarget;
        // private RenderTargetHandle HairAATangentDepthRT = new RenderTargetHandle("HairAATangentDepthRT");

        private List<ShaderTagId> shaderTagIdList = new List<ShaderTagId> {
        new ShaderTagId("UniversalForward"),
    };
 
        public Material hairAARenderer_SourceMat;
        public Material hairAARendererMat;
        
        RTHandle cameraColorAttachment;//设置当前渲染目标
        private RenderTextureDescriptor cameraColorAttachmentDescriptor;


        public HairAARendererPass(HairAARendererFeature feature)
        {
            this.feature = feature;
            // 在构造函数中创建描边时使用的材质
            this.hairAARendererMat = feature.hairAARendererMat;
            this.hairAARenderer_SourceMat = feature.hairAARenderer_SourceMat;
            HairAATangentRT.Init("HairAATangentRT");

        }

        public void SetTarget(RTHandle cameraColor, RTHandle cameraDepth)
        {
            this.cameraColor = cameraColor;
            this.cameraDepth = cameraDepth;
        }
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cameraColorAttachmentDescriptor.width = cameraTextureDescriptor.width;
            cameraColorAttachmentDescriptor.height = cameraTextureDescriptor.height;
            cameraColorAttachmentDescriptor.colorFormat = cameraTextureDescriptor.colorFormat;
            cameraColorAttachmentDescriptor.dimension = cameraTextureDescriptor.dimension;
            cameraColorAttachmentDescriptor.msaaSamples = cameraTextureDescriptor.msaaSamples;

            RenderingUtils.ReAllocateIfNeeded(ref cameraColorAttachment, cameraColorAttachmentDescriptor);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            //Get RT
            RenderTextureDescriptor cameraTextureDescriptor_full = renderingData.cameraData.cameraTargetDescriptor;
            cameraTextureDescriptor_full.width = Mathf.RoundToInt(cameraTextureDescriptor_full.width);
            cameraTextureDescriptor_full.height = Mathf.RoundToInt(cameraTextureDescriptor_full.height);
            cameraTextureDescriptor_full.graphicsFormat = feature.gfxFormat;
            // cameraTextureDescriptor_full.depthBufferBits = 16;
            var depthAttachment = renderingData.cameraData.renderer.cameraDepthTarget;
            
            cmd.GetTemporaryRT(HairAATangentRT.id, cameraTextureDescriptor_full, FilterMode.Bilinear);//创建RT-ID / RT desciptor /Filter Mode
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var stac = VolumeManager.instance.stack;

            CommandBuffer cmd = CommandBufferPool.Get("HairAA");

            {
                // 指定 DrawingSettings，这里使用了 URP 默认的 Shader Pass
                uint OutlineLayer = (uint)1 << 3;
                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, -1, OutlineLayer);
                DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                //覆盖layer里所有物体的材质
                // drawingSettings.overrideShader = Shader.Find("HairAA/Source");
                drawingSettings.overrideMaterial = hairAARenderer_SourceMat;

                RendererListParams rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                // var depthAttachment = renderingData.cameraData.renderer.cameraDepthTarget;
                cmd.SetRenderTarget(HairAATangentRT.id, cameraDepth);
                cmd.ClearRenderTarget(false, true, Color.clear, 0);

                // // 构建 RendererList
                RendererList rendererList = context.CreateRendererList(ref rendererListParams);

                // // 绘制需要描边的物体
                cmd.DrawRendererList(rendererList);

                context.ExecuteCommandBuffer(cmd);

                cmd.Clear();


                // 绘制描边
                hairAARendererMat.SetTexture("_CameraColor", cameraColor);
                cmd.Blit(HairAATangentRT.Identifier(), cameraColorAttachment, hairAARendererMat,0);
                cmd.Blit(cameraColorAttachment, cameraColor, hairAARendererMat,1);
                // Blitter.BlitCameraTexture(cmd, cameraColorAttachment, cameraColor);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                //     // 执行

            }
            // 回收 CommandBuffer
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {

            cmd.ReleaseTemporaryRT(HairAATangentRT.id);


        }
    }


    public override void Create()
    {

        // 建立对应的 ScriptableRenderPass
        hairAARendererPass = new HairAARendererPass(this);
        // 将 ScriptableRenderPass 的渲染时机指定为所有其他渲染操作完成之后
        hairAARendererPass.renderPassEvent = Event;
        const FormatUsage usage = FormatUsage.Linear | FormatUsage.Render;
        gfxFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R8G8B8A8_SRGB, usage) ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R16G16B16A16_SFloat; // HDR fallback

    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;

        if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView) return;
        if (camera.cameraType == CameraType.Game && camera.CompareTag("MainCamera") == false) //game视图目前只支持主相机
            return;
        renderer.EnqueuePass(hairAARendererPass);
    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        hairAARendererPass.SetTarget(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);
    }
}
