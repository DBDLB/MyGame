using UnityEngine;
using UnityEditor;
using System.IO;

public class CreatRendererFeatureFrame : EditorWindow
{
    private string fileName = "New";
    private string fileNameExtension = "RendererFeature";
    private string fileExtension = ".cs";

    [MenuItem("Tools/Create/RendererFeatureFrame", false, 0)]
    static void Init()
    {
        // 显示窗口
        CreatRendererFeatureFrame window = (CreatRendererFeatureFrame)EditorWindow.GetWindow(typeof(CreatRendererFeatureFrame));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Generate Custom Script", EditorStyles.boldLabel);

        GUILayout.Space(10);

        fileName = EditorGUILayout.TextField("Feature Name:", fileName);

        GUILayout.Space(10);

        if (GUILayout.Button("Create Script"))
        {
            GenerateCustomScript(fileName, fileNameExtension, fileExtension);
            Close();
        }
    }

    static void GenerateCustomScript(string fileName, string fileNameExtension, string fileExtension)
    {
        // 获取用户选择的文件夹路径
        string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (selectedPath == "")
        {
            Debug.LogError("Please select a folder in the Project window.");
            return;
        }

        // 创建一个新的.cs文件的路径
        string fullPath = Path.Combine(selectedPath, fileName + fileNameExtension + fileExtension);

        // 检查文件是否已经存在
        if (File.Exists(fullPath))
        {
            Debug.LogError("File " + fileName + fileNameExtension + fileExtension + " already exists in " + selectedPath);
            return;
        }

        // 从预定义的文本创建脚本内容
        string scriptContent = GenerateScriptContent(fileName);

        // 写入文件
        File.WriteAllText(fullPath, scriptContent);

        // 刷新Unity资源数据库，使新文件在Project窗口中可见
        AssetDatabase.Refresh();

        Debug.Log("Generated custom script: " + fileName + fileNameExtension + fileExtension + " in " + selectedPath);
    }

    // 生成自定义脚本的内容
    static string GenerateScriptContent(string className)
    {
        return
@"using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class " + className + @"RendererFeature : ScriptableRendererFeature
{

    [System.Serializable]
    public class " + className + @"RendererFeatureSettings
    {
        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader shader;
    }

    [SerializeField]
    " + className + @"RendererFeatureSettings m_settings = new " + className + @"RendererFeatureSettings();
    " + className + @"RendererFeaturePass m_pass;
    Material m_material;

    //管线初始化时,创建RenderFeature时调用,用于初始化Feature
    public override void Create()
    {
        if (m_pass == null)
        {
            m_pass = new " + className + @"RendererFeaturePass();
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isPreviewCamera) return;
        if (renderingData.cameraData.isSceneViewCamera) return;
        if (!GetMaterials())
        {
            Debug.LogErrorFormat(""{0}.AddRenderPasses(): Missing material. {1} render pass will not be added."", GetType().Name, name);
            return;
        }
        bool shouldAdd = m_pass.Setup(ref m_settings, ref m_material);
        if (shouldAdd)
        {
            renderer.EnqueuePass(m_pass);
        }
    }

    //管线资源销毁时调用,用于释放Feature的资源
    protected override void Dispose(bool disposing)
    {
        m_pass?.Dispose();
        m_pass = null;
        CoreUtils.Destroy(m_material);
    }

    private bool GetMaterials()
    {   
        if (m_material == null && m_settings != null)
        {
            m_material = CoreUtils.CreateEngineMaterial(m_settings.shader);
        }
        return m_material != null;
    }

    class " + className + @"RendererFeaturePass: ScriptableRenderPass
    {
        const string tag = """ + className + @""";
    
        " + className + @"RendererFeatureSettings m_settings;
        Material m_material;
        ProfilingSampler m_sampler = new ProfilingSampler(tag);
    
        RTHandle m_tempRT;
    
        RTHandle cameraTarget;
    
        public " + className + @"RendererFeaturePass()
        {
    
        }
    
        public bool Setup(ref " + className + @"RendererFeatureSettings settings, ref Material material)
        {
            if (settings.passEvent > RenderPassEvent.BeforeRenderingPostProcessing)
            {
                this.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            }
            else
            {
                this.renderPassEvent = settings.passEvent;
            }
            m_settings = settings;
            m_material = material;
            return m_material != null;
        }
        
        //管线准备开始为每个相机渲染场景之前调用
        //主要用来获取RT和设置渲染目标
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref m_tempRT, desc, name: """ + className + @"Temp"");
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_material == null)
            {
                Debug.LogErrorFormat(""{0}.Execute(): Missing material. Pass will not execute. Check for missing reference in the renderer resources."", GetType().Name);
                return;
            }
        
            var cmd = CommandBufferPool.Get();
            cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
        
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            using (new ProfilingScope(cmd, m_sampler))
            {
                Blitter.BlitCameraTexture(cmd, cameraTarget, m_tempRT, m_material, 0);
                Blitter.BlitCameraTexture(cmd, m_tempRT, cameraTarget);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        //每帧渲染完成后调用
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cameraTarget = null;
        }
        
        public void Dispose()
        {
            m_tempRT?.Release();
        }
    }
}";
    }
}