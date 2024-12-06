using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AntTrack : MonoBehaviour
{
    public LayerMask groundLayer; // 指定Ground层
    public int amountRadio = 20;
    public List<Vector3> mousePositions = new List<Vector3>(); // 记录鼠标位置的列表
    public List<Vector3> smoothMousePositions = new List<Vector3>(); // 平滑后的鼠标位置列表
    public float minDistance = 0.5f; // 最小距离阈值

    [Serializable]
    public class AntPath
    {
        public List<Vector3> pathList;
        public List<Ant> ants = new List<Ant>();
        public LineRenderer lineRenderer;
        public AntPath(List<Vector3> path, LineRenderer lineRenderer)
        {
            this.pathList = path;
            this.lineRenderer = lineRenderer;
        }
    }
    
    public List<AntPath> AntPathList = new List<AntPath>(); // 蚂蚁路径列表
    
    
    public GameObject linePrefab; // 线段预制体
    public Color lineColor; // 线段颜色
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
                        smoothMousePositions.Clear();
                        lineMaterial = new Material(Shader.Find("Unlit/Unlit"));
                        line = Instantiate(linePrefab, this.transform);
                        lineRenderer = line.GetComponent<LineRenderer>();
                        lineMaterial.SetColor("_BaseColor", lineColor);
                        lineMaterial.SetOverrideTag("RenderType", "TransparentCutout");
                        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        lineMaterial.SetInt("_ZWrite", 1);
                        lineMaterial.EnableKeyword("_ALPHATEST_ON");
                        //material.DisableKeyword("_ALPHABLEND_ON");
                        lineMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                        lineRenderer.material = lineMaterial;
                    }
                }

                // 创建一条从摄像机到鼠标指针的射线
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // 如果射线击中了Ground层的物体
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
                {
                    if (mousePositions.Count == 0)
                    {
                        mousePositions.Add(AntColony.instance.transform.position);
                    }

                    float distance = Vector3.Distance(mousePositions[mousePositions.Count - 1], hit.point);

                    while (distance > minDistance)
                    {
                        Vector3 pos = Vector3.MoveTowards(mousePositions[mousePositions.Count - 1], hit.point, minDistance);
                        pos.y = AntColony.instance.transform.position.y;
                        mousePositions.Add(pos);
                        
                        distance-=minDistance;
                    }
                    // 记录鼠标位置
                    // if (Vector3.Distance(mousePositions[mousePositions.Count - 1], hit.point) > minDistance)
                    // {
                    //     Vector3 pos = hit.point;
                    //     pos.y = 0;
                    //     mousePositions.Add(pos);
                    // }
                }
                PathHelper.GetWayPoints(mousePositions.ToArray(), amountRadio, ref smoothMousePositions);
                lineRenderer.positionCount = smoothMousePositions.Count;
                lineRenderer.SetPositions(smoothMousePositions.ToArray());
                lineMaterial.SetVector("_BaseMap_ST", new Vector4(mousePositions.Count, 1, 0, 0));
                lineMaterial.SetTexture("_BaseMap", lineTexture);
            }

            if (Input.GetMouseButtonUp(0))
            {
                // isDrawLine = false;

                if (mousePositions != null && mousePositions.Count > 2)
                {
                    ClickToShowUI.CreatingPath = this;
                    ClickToShowUI.Instance.CreateButton.SetActive(true);
                    ClickToShowUI.Instance.CancelCreateButton.SetActive(true);
                    Vector3 segmentDir = smoothMousePositions[smoothMousePositions.Count - 1] - smoothMousePositions[smoothMousePositions.Count - 2];
                    Vector3 perpendicular = Vector3.Cross(segmentDir, Vector3.up);
                    ClickToShowUI.Instance.CreateButton.transform.position = smoothMousePositions[smoothMousePositions.Count - 2] + perpendicular.normalized * 1f;
                    ClickToShowUI.Instance.CancelCreateButton.transform.position = smoothMousePositions[smoothMousePositions.Count - 2] - perpendicular.normalized * 1f;
                }
                else
                {
                    Destroy(line);
                }
            }
        }
    }
    public void CreatePath()
    {
        AntPathList.Add(new AntPath(mousePositions, lineRenderer));
        line = null;
        mousePositions = new List<Vector3>();
        this.enabled = false;
        AntColony.instance.GetComponent<ClickToShowUI>().enabled = true;
    }
    public void CancelCreatePath()
    {
        Destroy(line);
        line = null;
        mousePositions = new List<Vector3>();
        this.enabled = false;
        AntColony.instance.GetComponent<ClickToShowUI>().enabled = true;
    }
}
