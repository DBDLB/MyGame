using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
[RequireComponent(typeof(SDFMaker))]
public class TraversePainter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Collider targetCollider;
    [SerializeField] private GameObject targetQuad;
    [SerializeField] private GameObject paintBrushHead;
    [SerializeField] private GameObject paintBrushTex;
    [SerializeField] private RenderTexture rt;
    [SerializeField] private Material flowCameraMaterial;
    private Texture2D activeTex;
    private float brushSize = 1.0f;
    private float brushIncrement = 0.1f;

    private int instantiatedCount = 0;
    bool wasMouseDown = false;
    private List<GameObject> instantiated;
    

    void OnEnable()
    {
        instantiated = new List<GameObject>();
        activeTex = SaveRT();
    }
    
    void Update()
    {
        bool isMouseDown = Input.GetMouseButton(0)  || Input.GetMouseButton(1);
        PaintAtPosition(isMouseDown);
        if((wasMouseDown == true && isMouseDown == false) || instantiatedCount >= 1000){
            paintBrushHead.SetActive(false);
            Invoke("CollapsePainting", 0.1f);
        }
        brushSize = brushSize + Input.mouseScrollDelta.y * brushIncrement;
        brushSize = Mathf.Clamp(brushSize, 0.1f, 4.0f);

        wasMouseDown = isMouseDown;
    }

    void PaintAtPosition(bool isMouseDown){
        Vector3 mousePos = Input.mousePosition;
        Ray targetRay = targetCamera.ScreenPointToRay(mousePos);
        RaycastHit hitResult;
        if(Physics.Raycast(targetRay, out hitResult, 9999.0f)){
            if(hitResult.collider == GetComponent<Collider>()){
                Vector2 hitUV = hitResult.textureCoord;
                Vector3 paintPos;
                if(SetBrushHeadPos(hitUV, out paintPos) && isMouseDown){
                    PaintOnCanvas(paintPos);
                }
            }
        }
        Debug.Log(mousePos);
    }

    bool SetBrushHeadPos(Vector2 uv, out Vector3 worldPos){
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

    void PaintOnCanvas(Vector3 pos){

        GameObject instantiatedPaint = Instantiate(paintBrushTex);
        instantiatedPaint.transform.position = pos;
        instantiatedPaint.transform.localScale = new Vector3(brushSize, brushSize, brushSize);
        instantiated.Add(instantiatedPaint);
        instantiatedCount += 1;
        if(Input.GetMouseButton(1)){
            instantiatedPaint.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 1);
        }
        
    }

    void EnableBrushHead(){
        paintBrushHead.SetActive(true);
    }

    void CollapsePainting(){
        activeTex = SaveRT();
        instantiatedCount = 0;
        foreach(GameObject go in instantiated){
            Destroy(go);
        }
        instantiated.Clear();
        Invoke("EnableBrushHead", 0.1f);
    }


    Texture2D SaveRT(){
        Texture2D tex2d = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex2d.Apply();
        flowCameraMaterial.SetTexture("_MainTex", tex2d);
        return tex2d;
    }
    
    //[ContextMenu("SaveRT")]
    void ExportRT(){
        byte[] bytes;
        bytes = activeTex.EncodeToPNG();
        
        string path = "./Assets/_AAAAAAMy_test111111111111111111111111111111111111/ComputeShader/FishTraversal_new_1.png";
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        Debug.Log("Saved to " + path);
    }

    [ContextMenu("Compute SDF")]
    void ComputeSDF(){
        SaveRT();
        Invoke("GenerateAndSaveSDF", 0.1f);
    }

    void GenerateAndSaveSDF(){
        RenderTexture result = GetComponent<SDFMaker>().ComputeSDF(activeTex);
        Texture2D tex2d = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = result;
        tex2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex2d.Apply();

        byte[] bytes;
        bytes = tex2d.EncodeToPNG();
        string path = @".\Packages\Athena\Package\com.pwrd.athena-framework.renderer\Extensions\FishShoal\FishFlock\Editor\FishSDF_TEST.png";
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        Debug.Log("Saved SDF to " + path);
    }

}
