using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteInEditMode]
public class FlyingAntTrack : MonoBehaviour
{
    public LayerMask groundLayer; // 指定Ground层
    // public int amountRadio = 20;
    // public List<Vector3> mousePositions = new List<Vector3>(); // 记录鼠标位置的列表
    // public List<Vector3> smoothMousePositions = new List<Vector3>(); // 平滑后的鼠标位置列表
    // public float minDistance = 0.5f; // 最小距离阈值
    private Vector3 mousePosition;
    public List<Vector3> path;
    static public AntColony.VariousAnt flyingAnt;

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
            if (Input.GetMouseButtonUp(0))
            {
                //创建linePrefab
                if (line == null)
                {
                    if (linePrefab != null)
                    {
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
                mousePosition = Input.mousePosition;
                // 创建一条从摄像机到鼠标指针的射线
                Ray ray = Camera.main.ScreenPointToRay(mousePosition);
                RaycastHit hit;
                int tiling = 1;
                // 如果射线击中了Ground层的物体
                //如果点击的不是UI
                if (!EventSystem.current.IsPointerOverGameObject()&&flyingAnt.antPool.Count>0 && Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
                {
                    if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Ground"))
                    {
                        path.Add(AntColony.instance.transform.position);
                        Vector3 pos = hit.point;
                        pos.y = 7;
                        // path = CreateBezierCurve.GetBezierPath(transform.position, targetEnemy.position, Random.Range(30, 65), 10);
                        float randomAngle = UnityEngine.Random.Range(-65, 65);
                        if (Mathf.Abs(randomAngle) < 30)
                        {
                            randomAngle = randomAngle > 0 ? 30 : -30;
                        }

                        path.AddRange( CreateBezierCurve.GetBezierPath(transform.position, pos, randomAngle, 2,false));
                        path.AddRange(CreateBezierCurve.GetBezierPath(pos, transform.position, randomAngle, 2,false));
                        tiling = path.Count;
                        PathHelper.GetWayPoints(path.ToArray(), 10, ref path);
                        lineRenderer.positionCount = path.Count;
                        lineRenderer.SetPositions(path.ToArray());
                        lineMaterial.SetVector("_BaseMap_ST", new Vector4(path.Count, 1, 0, 0));
                        lineMaterial.SetTexture("_BaseMap", lineTexture);

                        GameObject ant = flyingAnt.antPool.Dequeue();
                        ant.GetComponent<FlyingAnt>().waypoint = new AntTrack.AntPath(new List<Vector3>(path), lineRenderer);
                        ant.GetComponent<FlyingAnt>().attackPosition = hit.point;
                        ant.SetActive(true);

                    }
                }

                // PathHelper.GetWayPoints(mousePositions.ToArray(), amountRadio, ref smoothMousePositions);

                
                line = null;
                path.Clear();
            }
        }
    }
    
    public void FlyingAntSetEnable(Image image)
    {
        this.enabled = !this.enabled;
        //设置Color Multiplier
        image.color = this.enabled ? new Color(1,0.62f,0,1f) : new Color(1,0.62f,0,0.6f);
    }
    
    public void FlyingAntSetDisable(Image image)
    {
        this.enabled = false;
        //设置Color Multiplier
        image.color = new Color(1,0.62f,0,0.6f);
    }
}
