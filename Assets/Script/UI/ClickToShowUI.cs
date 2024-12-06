using System.Collections.Generic;
using UnityEngine;

public class ClickToShowUI : MonoBehaviour
{
    
    public static ClickToShowUI instance;
    public static ClickToShowUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ClickToShowUI>();
                if (instance == null)
                {
                    GameObject singleton = new GameObject(typeof(ClickToShowUI).Name);
                    instance = singleton.AddComponent<ClickToShowUI>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    
    public List<GameObject> uiPanels = new List<GameObject>();

    public float selectionThreshold = 0.1f; // 点击曲线的距离阈值
    private Vector3 PreviousPoint;
    private LineRenderer PreviousLineRenderer;
    public GameObject DeleteButton;
    public GameObject CreateButton;
    public GameObject CancelCreateButton;
    public AntTrack.AntPath antPath;
    public int variousAntIndex;

    void Start()
    {
        foreach (var uiPanel in uiPanels)
        {
            uiPanel.SetActive(false); // 初始化时隐藏UI面板
        }
    }

    void Update()
    {
        ShowUI(); // 显示UI面板
        
        SelectionCurve(); // 选择曲线
    }
    
    void ShowUI()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform)
                {
                    foreach (var uiPanel in uiPanels)
                    {
                        uiPanel.SetActive(true);
                    }
                }
                else if (hit.transform.gameObject.layer != LayerMask.NameToLayer("UI"))
                {
                    foreach (var uiPanel in uiPanels)
                    {
                        uiPanel.SetActive(false);
                    }
                }
            }
        }
    }

    void SelectionCurve()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Vector3 segmentStart = Vector3.zero;
            Vector3 segmentEnd = Vector3.zero;

            LayerMask groundLayer = LayerMask.GetMask("Ground","UI");
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                float NearestDistance = float.MaxValue;
                if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    for (int i= 0; i < AntColony.variousAnts.Count;i++)
                    {
                        foreach (var antPathList in AntColony.variousAnts[i].antTrack.AntPathList)
                        {
                            // 遍历所有曲线进行选择
                            Vector3 closestPoint = GetClosestPointOnLine(antPathList.lineRenderer, hit.point, out segmentStart, out segmentEnd);
                            float distance = Vector3.Distance(hit.point, closestPoint);
                            NearestDistance = Mathf.Min(NearestDistance, distance);
                            if (distance < selectionThreshold)
                            {
                                if (PreviousLineRenderer != null && (PreviousLineRenderer != antPathList.lineRenderer))
                                {
                                    DeleteButton.SetActive(false);
                                    HighlightLineRenderer(PreviousLineRenderer, 0.0f);
                                    // PreviousLineRenderer = null;
                                    // PreviousPoint = Vector3.zero;
                                }
                                PreviousPoint = hit.point;
                                PreviousLineRenderer = antPathList.lineRenderer;

                                Debug.Log("Selected Line: " + antPathList.lineRenderer.name);
                                HighlightLineRenderer(antPathList.lineRenderer, 1.0f);
                                antPath = antPathList;
                                variousAntIndex = i;
                                DeleteButton.SetActive(true);
                                //计算segmentStart和segmentEnd的垂直线
                                Vector3 segmentDir = segmentEnd - segmentStart;
                                Vector3 perpendicular = Vector3.Cross(segmentDir, Vector3.up);
                                DeleteButton.transform.position = closestPoint + perpendicular.normalized * 1f;
                                break; // 找到第一个目标后退出
                            }
                            else
                            {
                                HighlightLineRenderer(antPathList.lineRenderer, 0.0f);
                            }
                        }
                    }

                    if (NearestDistance > selectionThreshold)
                    {
                        DeleteButton.SetActive(false);
                    }
                }
            }
        }
    }
    
    public void DeletePath()
    {
        foreach (var ant in antPath.ants)
        {
            ant.waypoint = null;
        }
        AntColony.variousAnts[variousAntIndex].antTrack.AntPathList.Remove(antPath);
        Destroy(antPath.lineRenderer.gameObject);
        DeleteButton.SetActive(false);
    }

    public static AntTrack CreatingPath;
    
    public void CreatePath()
    {
        CreatingPath.CreatePath();
        CreateButton.SetActive(false);
        CancelCreateButton.SetActive(false);
        CreatingPath = null;
    }
    public void CancelCreatePath()
    {
        CreatingPath.CancelCreatePath();
        CreateButton.SetActive(false);
        CancelCreateButton.SetActive(false);
        CreatingPath = null;
    }
    
    Vector3 GetClosestPointOnLine(LineRenderer lineRenderer, Vector3 point,out Vector3 OutSegmentStart, out Vector3 OutSegmentEnd)
    {
        Vector3 closestPoint = lineRenderer.GetPosition(0);
        float closestDistance = float.MaxValue;

        Vector3 finallySegmentStart = Vector3.zero;
        Vector3 finallySegmentEnd = Vector3.zero;
        for (int i = 0; i < lineRenderer.positionCount - 1; i++)
        {
            Vector3 segmentStart = lineRenderer.GetPosition(i);
            Vector3 segmentEnd = lineRenderer.GetPosition(i + 1);
            Vector3 segmentClosestPoint = GetClosestPointOnSegment(segmentStart, segmentEnd, point);
            point.y = segmentClosestPoint.y; // 保持 y 坐标一致
            float distance = Vector3.Distance(point, segmentClosestPoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = segmentClosestPoint;
                finallySegmentStart = segmentStart;
                finallySegmentEnd = segmentEnd;
            }
        }
        OutSegmentStart = finallySegmentStart;
        OutSegmentEnd = finallySegmentEnd;
        return closestPoint;
    }

    Vector3 GetClosestPointOnSegment(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 lineDir = end - start;
        float lengthSquared = lineDir.sqrMagnitude;
        if (lengthSquared == 0) return start; // 线段的起点和终点重合

        float t = Vector3.Dot(point - start, lineDir) / lengthSquared;
        t = Mathf.Clamp01(t); // 限制 t 在 [0, 1] 之间
        return start + t * lineDir;
    }

    void HighlightLineRenderer(LineRenderer lineRenderer, float mode)
    {
        lineRenderer.material.SetFloat("_SelectedMode", mode);
    }
    
    // 隐藏所有面板
    public void HideAllPanels()
    {
        foreach (GameObject panel in uiPanels)
        {
            panel.SetActive(false);
        }
    }
}