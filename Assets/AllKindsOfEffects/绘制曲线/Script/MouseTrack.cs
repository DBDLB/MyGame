using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MouseTrack : MonoBehaviour
{
    public LayerMask groundLayer; // 指定Ground层
    public int amountRadio = 20;
    public List<Vector3> mousePositions = new List<Vector3>(); // 记录鼠标位置的列表
    public List<Vector3> smoothMousePositions = new List<Vector3>(); // 平滑后的鼠标位置列表
    
    public float minDistance = 0.5f; // 最小距离阈值
    public static List<List<Vector3>> AntPathList = new List<List<Vector3>>();
    
    public GameObject linePrefab; // 线段预制体
    public Texture lineTexture; // 线段纹理
    
    // private bool isDrawLine = false;
    private GameObject line;


    // Update is called once per frame
    LineRenderer lineRenderer;
    Material lineMaterial;

    // public void SetIsDrawLine(bool isDrawLine)
    // {
    //     this.isDrawLine = isDrawLine;
    // }
    
    void Update()
    {
        // if (isDrawLine)
        {
            if (Input.GetMouseButton(0))
            {
                //创建linePrefab
                if (line == null)
                {
                    if (linePrefab != null)
                    {
                        lineMaterial = new Material(Shader.Find("Unlit/Transparent"));
                        line = Instantiate(linePrefab, this.transform);
                        lineRenderer = line.GetComponent<LineRenderer>();
                        lineRenderer.material = lineMaterial;
                    }
                }

                // 创建一条从摄像机到鼠标指针的射线
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // 如果射线击中了Ground层的物体
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
                {
                    // 记录鼠标位置
                    if (mousePositions.Count == 0 ||
                        Vector3.Distance(mousePositions[mousePositions.Count - 1], hit.point) > minDistance)
                    {
                        mousePositions.Add(hit.point);
                    }
                }
                PathHelper.GetWayPoints(mousePositions.ToArray(), amountRadio, ref smoothMousePositions);
                lineRenderer.positionCount = smoothMousePositions.Count;
                lineRenderer.SetPositions(smoothMousePositions.ToArray());
                lineMaterial.SetVector("_MainTex_ST", new Vector4(mousePositions.Count, 1, 0, 0));
                lineMaterial.SetTexture("_MainTex", lineTexture);
            }

            if (Input.GetMouseButtonUp(0))
            {
                // isDrawLine = false;
                AntPathList.Add(mousePositions);
                line = null;
                mousePositions = new List<Vector3>();
                this.enabled = false;
            }
        }
    }

    // 获取记录的鼠标位置列表
    public List<Vector3> GetMousePositions()
    {
        return mousePositions;
    }
}
