using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;
public class FishFlockSettler : EditorWindow
{
    [MenuItem("美术工具/海底鱼群工具", false, 404)]
    private static void ShowWindow()
    {
        var window = GetWindow<FishFlockSettler>();
        window.titleContent = new GUIContent("海底鱼群工具");
        window.Show();
    }
    
    private bool showFishPath = false;
    private bool startDrawing = false;
    private GameObject DrawPlane;
    private GameObject FishFlock;
    private GameObject RenderTextureMaker;
    public Vector3 mousePos;
    
    private List<GameObject> allFishFlocks = new List<GameObject>();
    public Vector3 fishFlocksPosition = new Vector3(0, 0, 0);

    string FishFlockPath = "Assets/AllKindsOfEffects/simulation_of_fish_school(鱼群模拟)/FishFlock/Runtime/Prefab/FishFlock.prefab";
    string DrawPlanePath = "Assets/AllKindsOfEffects/simulation_of_fish_school(鱼群模拟)/FishFlock/Editor/Prefab/DrawPlane.prefab";
    string RtPath = "Assets/AllKindsOfEffects/simulation_of_fish_school(鱼群模拟)/FishFlock/Editor/MovementTex.renderTexture";
    string RenderTextureMakerPath = "Assets/AllKindsOfEffects/simulation_of_fish_school(鱼群模拟)/FishFlock/Editor/Prefab/RenderTextureMaker.prefab";
    
    private GUIStyle startDrawingButtonStyle;
    private RenderTexture rt;
    
    
    private Camera renderCamera;
    private RenderTexture renderTexture;
    
    Texture2D newfishSDF;
    private Texture2D previousNewfishSDF;
    private Collider targetCollider;
    private GameObject targetQuad;
    private GameObject paintBrushHead;
    private GameObject paintBrushTex;
    private Material flowCameraMaterial;
    private Texture2D activeTex;
    private float brushSize = 1.0f;
    private float brushIncrement = 0.1f;
    private int instantiatedCount = 0;
    private List<GameObject> instantiated;
    private ComputeShader compute;
    private string folderPath = "";
    private string fileName = "";
    
    GameObject nowDrawPlane;
    Rect labelRect;
    private Vector2 scrollPosition;
    
    string flowCameraMaterialPath = "Assets/AllKindsOfEffects/simulation_of_fish_school(鱼群模拟)/FishFlock/Editor/FlowCameraTex.mat";
    string paintBrushTexPath = "Assets/AllKindsOfEffects/simulation_of_fish_school(鱼群模拟)/FishFlock/Editor/Prefab/DecoPrefab.prefab";
    string computePath = "Assets/AllKindsOfEffects/simulation_of_fish_school(鱼群模拟)/FishFlock/Editor/Scripts/JumpFloodSDF.compute";
    string SDFPath = "Assets/AllKindsOfEffects/simulation_of_fish_school(鱼群模拟)/FishFlock/Demo/Textures/FishSDF.png";

    private TextureImporter textureImporter;
    
    
    private void OnEnable()
    {
        renderTexture = new RenderTexture(512, 512, 24);
        DrawPlane = AssetDatabase.LoadAssetAtPath<GameObject>(DrawPlanePath);
        rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RtPath);
        ClearRT(rt);
        RenderTextureMaker = AssetDatabase.LoadAssetAtPath<GameObject>(RenderTextureMakerPath);
        
        RenderTextureMaker = PrefabUtility.InstantiatePrefab(RenderTextureMaker) as GameObject;
        PrefabUtility.UnpackPrefabInstance(RenderTextureMaker, PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction);

        targetQuad = FindAllChildrenByName(RenderTextureMaker.transform, "Quad")[0].gameObject;
        targetCollider = targetQuad.GetComponent<Collider>();
        paintBrushHead = FindAllChildrenByName(RenderTextureMaker.transform, "PaintBrush")[0].gameObject;
        compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(computePath);
        flowCameraMaterial = AssetDatabase.LoadAssetAtPath<Material>(flowCameraMaterialPath);
        paintBrushTex = AssetDatabase.LoadAssetAtPath<GameObject>(paintBrushTexPath);
        newfishSDF = AssetDatabase.LoadAssetAtPath<Texture2D>(SDFPath);
        previousNewfishSDF = newfishSDF;
        
        instantiated = new List<GameObject>();
        activeTex = SaveRT();
        labelRect = new Rect(0, 130, renderTexture.width, renderTexture.height);
    }
    
    private void OnGUI()
    {
        if (DrawPlane == null)
        {
            DrawPlane = AssetDatabase.LoadAssetAtPath<GameObject>(DrawPlanePath);
        }
        if (rt == null)
        {
            rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RtPath);
        }
        
        if(GUILayout.Button("创建鱼群"))
        {
            if (FishFlock == null)
            {
                FishFlock = AssetDatabase.LoadAssetAtPath<GameObject>(FishFlockPath);
            }

            if (FishFlock != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(FishFlock) as GameObject;
                if (instance != null)
                {
                    Debug.Log("鱼群创建成功");
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    instance.transform.position = fishFlocksPosition;
                    Flocker flocker = instance.GetComponent<Flocker>();
                    flocker.enabled = true;
                    flocker.fishSDF = newfishSDF;
                }
                else
                {
                    Debug.LogError("鱼群创建失败");
                }
            }
            else
            {
                Debug.LogError("在路径加载预制件失败: " + FishFlockPath);
            }
        }
        
        // 开始滚动视图
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        fishFlocksPosition = EditorGUILayout.Vector3Field("创建鱼群位置", fishFlocksPosition, GUILayout.Width(300));
        GUILayout.Space(10);
        
        showFishPath = EditorGUILayout.ToggleLeft("显示鱼群路径", showFishPath);
        if (showFishPath)
        {
            GetFishFlock();
            if (allFishFlocks.Count > 0)
            {
                foreach (GameObject fishFlock in allFishFlocks)
                {
                    Flocker flocker = fishFlock.GetComponent<Flocker>();
                    if (flocker)
                    {
                        if (DrawPlane != null)
                        {
                            if (fishFlock.transform.Find(DrawPlane.name) == null)
                            {
                                GameObject drawPlane = PrefabUtility.InstantiatePrefab(DrawPlane) as GameObject;
                                if (drawPlane != null)
                                {
                                    PrefabUtility.UnpackPrefabInstance(drawPlane, PrefabUnpackMode.Completely,
                                        InteractionMode.AutomatedAction);
                                    drawPlane.transform.SetParent(fishFlock.transform);
                                    drawPlane.transform.localPosition = new Vector3(0, -0.5f, 0);
                                    drawPlane.transform.localRotation = Quaternion.identity;
                                    drawPlane.transform.localScale = Vector3.one * 0.1f;
                                    //新建一个临时材质球 
                                    Material material = new Material(Shader.Find("Unlit/Texture"));
                                    drawPlane.GetComponent<MeshRenderer>().sharedMaterial = material;
                                    if (material != null)
                                    {
                                        if (!startDrawing)
                                        {
                                            Texture2D texture2D = flocker.fishSDF;
                                            material.SetTexture("_MainTex", texture2D);
                                        }
                                    }

                                }
                                else
                                {
                                    Debug.LogError("鱼群路径显示失败");
                                }
                            }
                            else
                            {
                                Transform drawPlane = fishFlock.transform.Find(DrawPlane.name);
                                if (drawPlane != null)
                                {
                                    if (!startDrawing)
                                    {
                                        drawPlane.gameObject.SetActive(true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            //隐藏DrawPlane
            GetFishFlock();
            if (allFishFlocks.Count > 0)
            {
                foreach (GameObject fishFlock in allFishFlocks)
                {
                    Flocker flocker = fishFlock.GetComponent<Flocker>();
                    if (flocker)
                    {
                        Transform drawPlane = fishFlock.transform.Find(DrawPlane.name);
                        if (drawPlane != null)
                        {
                            if (!startDrawing)
                            {
                                drawPlane.gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }
        }
        GUILayout.Space(10);
        
        //创建一个开始绘制按钮,点击后按钮变绿
        if (startDrawingButtonStyle == null)
        {
            startDrawingButtonStyle = new GUIStyle(GUI.skin.button);
            startDrawingButtonStyle.normal.textColor = Color.black;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("开始绘制(选中鱼群)",startDrawingButtonStyle, GUILayout.Width(150)))
        {
            startDrawing = !startDrawing;
            startDrawingButtonStyle.normal.textColor = startDrawing ? Color.green : Color.black;

            if (startDrawing)
            {
                GetFishFlock();
                if (allFishFlocks.Count > 0)
                {
                    foreach (GameObject fishFlock in allFishFlocks)
                    {                
                        List<Transform> paintCameras = FindAllChildrenByName(fishFlock.transform, "PaintCamera");
                        foreach (Transform paintCamera in paintCameras)
                        {
                            paintCamera.gameObject.SetActive(false);
                        }
                        
                        Flocker flocker = fishFlock.GetComponent<Flocker>();
                        if (flocker)
                        {
                            if (DrawPlane != null)
                            {
                                if (fishFlock.transform.Find(DrawPlane.name) == null)
                                {
                                    GameObject drawPlane = PrefabUtility.InstantiatePrefab(DrawPlane) as GameObject;
                                    if (drawPlane != null)
                                    {
                                        PrefabUtility.UnpackPrefabInstance(drawPlane, PrefabUnpackMode.Completely,
                                            InteractionMode.AutomatedAction);
                                        nowDrawPlane = drawPlane;
                                        drawPlane.transform.SetParent(fishFlock.transform);
                                        drawPlane.transform.localPosition = new Vector3(0, -0.5f, 0);
                                        drawPlane.transform.localRotation = Quaternion.identity;
                                        drawPlane.transform.localScale = Vector3.one * 0.1f;
                                        //新建一个临时材质球 
                                        Material material = new Material(Shader.Find("Unlit/Texture"));
                                        drawPlane.GetComponent<MeshRenderer>().sharedMaterial = material;
                                        if (material != null)
                                        {
                                            material.SetTexture("_MainTex", rt);
                                            showFishPath = true;
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogError("鱼群路径显示失败");
                                    }
                                }
                                else
                                {
                                    Transform drawPlane = fishFlock.transform.Find(DrawPlane.name);
                                    if (drawPlane != null)
                                    {
                                        drawPlane.gameObject.SetActive(true);
                                        nowDrawPlane = drawPlane.gameObject;
                                        drawPlane.GetComponent<MeshRenderer>().sharedMaterial
                                            .SetTexture("_MainTex", rt);
                                        showFishPath = true;
                                    }
                                }
                            }
                        }
                    }

                    //选中物体
                    GameObject selectedObject = Selection.activeGameObject;
                    if (selectedObject != null)
                    {
                        foreach (Transform paintCamera in FindAllChildrenByName(selectedObject.transform, "PaintCamera"))
                        {
                            paintCamera.gameObject.SetActive(true);
                            renderCamera = paintCamera.GetComponent<Camera>();
                            renderCamera.orthographicSize = selectedObject.transform.localScale.x * 0.5f+10;
                            renderCamera.targetTexture = renderTexture;
                        }
                    }
                    else
                    {
                        GameObject paintCamera = FindAllChildrenByName(allFishFlocks[0].transform, "PaintCamera")[0].gameObject;
                        paintCamera.SetActive(true);
                        renderCamera = paintCamera.GetComponent<Camera>();
                        renderCamera.orthographicSize = selectedObject.transform.localScale.x * 0.5f+10;
                        renderCamera.targetTexture = renderTexture;
                    }
                }
            }
            else
            {
                //隐藏DrawPlane
                GetFishFlock();
                if (allFishFlocks.Count > 0)
                {
                    foreach (GameObject fishFlock in allFishFlocks)
                    {
                        Flocker flocker = fishFlock.GetComponent<Flocker>();
                        if (flocker)
                        {
                            Transform drawPlane = fishFlock.transform.Find(DrawPlane.name);
                            if (drawPlane != null)
                            {
                                drawPlane.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MainTex", flocker.fishSDF);
                            }
                        }
                    }
                }
            }
        }
        GUILayout.Space(10);
        if(GUILayout.Button("重画",GUILayout.Width(50)))
        {
            ClearRT(rt);
            activeTex = SaveRT();
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (renderTexture != null && startDrawing)
        {
            RTWindow();
            GUILayout.Space(532);
            GUILayout.Space(10);
        }
        
        GUILayout.Space(10);
        //提示是否保存笔画
        if (!isSave)
        {
            EditorGUILayout.HelpBox("未保存请右键保存笔画", MessageType.Warning);
        }
        
        // newfishSDF = (Texture2D)EditorGUILayout.ObjectField("选择一个Texture2D", newfishSDF, typeof(Texture2D), false);
        // if (newfishSDF != previousNewfishSDF)
        // {
        //     GetFishFlock();
        //     foreach (var fishFlock in allFishFlocks)
        //     {
        //         fishFlock.GetComponent<Flocker>().fishSDF = newfishSDF;
        //     }
        //     previousNewfishSDF = newfishSDF;
        // }

        
        
        fileName = EditorGUILayout.TextField("输入文件名", fileName);
        folderPath = FolderField("选择文件夹", folderPath);
        if (GUILayout.Button("创建鱼群路径图")&&fileName!=""&&folderPath!="")
        {
            paintBrushHead.SetActive(false);
            GenerateComputeSDF();
        }
        else
        {
            EditorGUILayout.HelpBox("请先输入文件名和选择文件夹,创建鱼群路径图前请先鼠标右键保存", MessageType.Warning);
        }
        
        // 结束滚动视图
        EditorGUILayout.EndScrollView();
    }
    
    private float timer = 0;
    private bool isSave = true;
    void RTWindow()
    {
        Event e = UnityEngine.Event.current;
        GUI.DrawTexture(labelRect, renderTexture);
        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
        {
            bool isMouseDown = (e.button == 0);
            PaintAtPosition(isMouseDown, nowDrawPlane.transform);
            Debug.Log(e.button);
            if((!isSave&&isMouseDown == false&& e.button == 1) || instantiatedCount >= 1000){
                // Invoke("CollapsePainting", 0.1f);
                timer += Time.deltaTime;
                paintBrushHead.SetActive(false);
                if (timer > 0.1f)
                {
                    timer = 0;
                    isSave = true;
                    CollapsePainting();
                }
            }

            if (e.button == 2)
            {
                paintBrushHead.SetActive(true);
                brushSize = brushSize + e.delta.x * brushIncrement*0.3f;
            }

            brushSize = Mathf.Clamp(brushSize, 0.1f, 4.0f);
            
            e.Use();
        }
    }


    //关闭窗口时
    private void OnDestroy()
    {
        //删除DrawPlane
        GetFishFlock();
        if (allFishFlocks.Count > 0)
        {
            foreach (GameObject selectedObject in allFishFlocks)
            {
                Flocker flocker = selectedObject.GetComponent<Flocker>();
                if (flocker)
                {
                    Transform drawPlane = selectedObject.transform.Find(DrawPlane.name);
                    if (drawPlane != null)
                    {
                        DestroyImmediate(drawPlane.gameObject);
                        Debug.Log("鱼群路径删除成功");
                    }
                }
            }
        }

        if (renderCamera != null)
        {
            DestroyImmediate(renderCamera.gameObject);
        }

        if (renderTexture != null)
        {
            DestroyImmediate(renderTexture);
        }

        if (RenderTextureMaker != null)
        {
            DestroyImmediate(RenderTextureMaker);
        }
        
        
        foreach(GameObject go in instantiated){
            DestroyImmediate(go);
        }

    }
    
    //创建一个文件夹选择框
    public static string FolderField(string label, string path)
    {
        GUILayout.BeginHorizontal();
        string newPath = EditorGUILayout.TextField(label, path);
        if (GUILayout.Button("Browse", GUILayout.Width(100)))
        {
            newPath = EditorUtility.OpenFolderPanel("Select Folder", newPath, "");
        }
        GUILayout.EndHorizontal();
        return newPath;
    }

    //获取场景中的所有鱼群
    private void GetFishFlock()
    {
        allFishFlocks.Clear();
        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                var fishFlocks = rootGameObject.GetComponentsInChildren<Flocker>(true);
                if (fishFlocks.Length > 0)
                {
                    allFishFlocks.AddRange(fishFlocks.Select(fishFlock => fishFlock.gameObject));
                }
            }
        }
    }
    
    //切换到Game视图的Display8
    private void ChangeToDisplay(int displayIndex)
    {
        Assembly unityEditorAssembly = typeof(EditorWindow).Assembly;
        Type gameViewType = unityEditorAssembly.GetType("UnityEditor.GameView");
        EditorWindow gameViewWindow = EditorWindow.GetWindow(gameViewType);

        MethodInfo setTargetDisplayMethod = gameViewType.GetMethod("SetTargetDisplay", BindingFlags.NonPublic | BindingFlags.Instance);
        setTargetDisplayMethod.Invoke(gameViewWindow, new object[] { displayIndex });
    }
    
    //递归查找特定名称的子物体
    public List<Transform> FindAllChildrenByName(Transform parentTransform, string childName)
    {
        List<Transform> foundChildren = new List<Transform>();

        foreach (Transform childTransform in parentTransform)
        {
            if (childTransform.name == childName)
            {
                foundChildren.Add(childTransform);
            }
            foundChildren.AddRange(FindAllChildrenByName(childTransform, childName));
        }

        return foundChildren;
    }
    
    void PaintAtPosition(bool isMouseDown,Transform drawPlane)
    {
        Vector3 mousePos = Event.current.mousePosition;
        mousePos = new Vector3(mousePos.x,mousePos.y-130,0);
        Ray targetRay = renderCamera.ScreenPointToRay(mousePos);
        RaycastHit hitResult;
        if(Physics.Raycast(targetRay, out hitResult, 9999.0f))
        {
            Collider test = drawPlane.GetComponent<Collider>();
            if(hitResult.transform.name == drawPlane.name)
            {
                Vector2 hitUV = hitResult.textureCoord;
                hitUV = new Vector2(hitUV.x, 1 - hitUV.y);
                Vector3 paintPos;
                if(SetBrushHeadPos(hitUV, out paintPos) && isMouseDown){
                    paintBrushHead.SetActive(true);
                    isSave = false;
                    PaintOnCanvas(paintPos);
                }
            }
        }
        Debug.Log(mousePos);
    }

    bool SetBrushHeadPos(Vector2 uv, out Vector3 worldPos)
    {
        if(paintBrushHead){
            uv.x = (uv.x - 0.5f);
            uv.y = (uv.y - 0.5f);
            paintBrushHead.transform.localScale = new Vector3(brushSize, brushSize, brushSize);
            Vector3 headPos = paintBrushHead.transform.position;
            if(targetQuad != null){
                float size = targetQuad.transform.localScale.x;
                Vector3 camPos = targetQuad.transform.position;
                Vector3 brushPos = new Vector3(camPos.x, camPos.y, headPos.z);
                brushPos.x += uv.x * size;
                brushPos.y += uv.y * size;
                paintBrushHead.transform.position = brushPos;
                brushPos.z += 0.5f;
                worldPos = brushPos;
                return true;
            }
        }
        worldPos = new Vector3(0, 0, 0);
        return false;
    }

    void PaintOnCanvas(Vector3 pos)
    {
        GameObject instantiatedPaint = Instantiate(paintBrushTex);
        instantiatedPaint.transform.position = pos;
        instantiatedPaint.transform.localScale = new Vector3(brushSize, brushSize, brushSize);
        instantiated.Add(instantiatedPaint);
        instantiatedCount += 1;
        if(Input.GetMouseButton(1)){
            instantiatedPaint.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 1);
        }
        
    }
    

    void CollapsePainting()
    {
        if (!paintBrushHead.activeSelf)
        {
            activeTex = SaveRT();
            instantiatedCount = 0;
            foreach(GameObject go in instantiated){
                DestroyImmediate(go);
            }
            instantiated.Clear();
        }
        // Invoke("EnableBrushHead", 0.1f);
    }


    Texture2D SaveRT()
    {
        Texture2D tex2d = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex2d.Apply();
        flowCameraMaterial.SetTexture("_MainTex", tex2d);
        return tex2d;
    }
    
    //将rt设置为黑色
    void ClearRT(RenderTexture tex)
    {
        RenderTexture.active = tex;
        GL.Clear(true, true, Color.black);
    }
    
    void GenerateComputeSDF(){
        GenerateAndSaveSDF();
        AssetDatabase.Refresh();
        GetFishFlock();
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject != null)
        {
            selectedObject.GetComponent<Flocker>().fishSDF = newfishSDF;
        }
    }

    void GenerateAndSaveSDF(){
        RenderTexture result = ComputeSDF(activeTex);
        Texture2D tex2d = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = result;
        tex2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex2d.Apply();

        byte[] bytes;
        bytes = tex2d.EncodeToPNG();
        string path = folderPath + "/" + fileName + ".png";
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        Debug.Log("Saved SDF to " + path);
        AssetDatabase.Refresh();
        

        //获取图片
        path=path.Replace(Application.dataPath, "Assets");
        newfishSDF= AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        textureImporter = (TextureImporter)AssetImporter.GetAtPath(path);
        if (textureImporter != null)
        {
            // 取消勾选sRGB
            textureImporter.sRGBTexture = false;
            // 重新导入图片以应用更改
            AssetDatabase.ImportAsset(textureImporter.assetPath, ImportAssetOptions.ForceUpdate);
        }
    }
    
    private RenderTexture rt1;
    private RenderTexture rt2;

    public RenderTexture ComputeSDF(Texture2D tex){
        rt1 = new RenderTexture(tex.width, tex.height, 24,RenderTextureFormat.ARGBFloat);
        rt1.enableRandomWrite = true;
        rt1.filterMode = FilterMode.Point;
        rt1.wrapMode = TextureWrapMode.Clamp;
        rt1.Create();

        rt2 = new RenderTexture(tex.width, tex.height, 24,RenderTextureFormat.ARGBFloat);
        rt2.enableRandomWrite = true;
        rt2.filterMode = FilterMode.Point;
        rt2.wrapMode = TextureWrapMode.Clamp;
        rt2.Create();

        compute.SetTexture(0, "_MainTex", rt1);
        compute.SetTexture(0, "_Previous", tex);
        compute.SetVector("_Dimensions", new Vector4(tex.width, tex.height, 0.0f, 1.0f));
        int xRound = Mathf.CeilToInt(tex.width / 8);
        int yRound = Mathf.CeilToInt(tex.height / 8);
        compute.Dispatch(0, xRound, yRound, 1);

        float stepWidth = tex.width * 0.5f;
        float stepHeight = tex.height * 0.5f;
        bool isFirstTextureUsed = true;
        while(stepWidth >= 1 || stepHeight >= 1){
            if(isFirstTextureUsed){
                Debug.Log("Step SDF 1st");
                StepSDF(rt1, rt2, stepWidth, stepHeight);
                isFirstTextureUsed = false;
            }
            else{
                Debug.Log("Step SDF 2nd");
                StepSDF(rt2, rt1, stepWidth, stepHeight);
                isFirstTextureUsed = true;
            }
            stepWidth *= 0.5f;
            stepHeight *= 0.5f;
        }
        RenderTexture lastUsed = isFirstTextureUsed? rt1 : rt2;
        RenderTexture result = ComputeInfluenceMap(lastUsed, tex);
        rt1.Release();
        rt2.Release();
        rt2 = null;
        rt1 = result;
        return rt1;
    }
    public void StepSDF(RenderTexture src, RenderTexture dest, float stepWidth, float stepHeight){
        
        int w = Mathf.RoundToInt(stepWidth);
        int h = Mathf.RoundToInt(stepHeight);
        compute.SetTexture(1, "_Previous", src);
        compute.SetTexture(1, "_MainTex", dest);
        compute.SetVector("_Dimensions", new Vector4(src.width, src.height, 0.0f, 1.0f));
        compute.SetVector("_StepSize", new Vector4(w, h, 0.0f, 1.0f));
        
        int xRound = Mathf.CeilToInt(src.width / 8);
        int yRound = Mathf.CeilToInt(src.height / 8);
        compute.Dispatch(1, xRound, yRound, 1);
    }
    
    public RenderTexture ComputeInfluenceMap(RenderTexture source, Texture2D baseMap){
        RenderTexture rt = new RenderTexture(source.width, source.height, 24);
        rt.enableRandomWrite = true;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();
        compute.SetTexture(2, "_Previous", source);
        compute.SetTexture(2, "_BaseMap", baseMap);
        compute.SetTexture(2, "_MainTex", rt); 
        compute.SetVector("_Dimensions", new Vector4(source.width, source.height, 0.0f, 1.0f));
        int xRound = Mathf.CeilToInt(source.width / 8);
        int yRound = Mathf.CeilToInt(source.height / 8);
        compute.Dispatch(2, xRound, yRound, 1);

        return rt;
    }
}
