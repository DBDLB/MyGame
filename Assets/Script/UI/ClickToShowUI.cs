using UnityEngine;

public class ClickToShowUI : MonoBehaviour
{
    public GameObject uiPanel; // 指向你的UI面板
    private Canvas canvas; // 用于将世界坐标转换为屏幕坐标

    void Start()
    {
        // 获取 Canvas 组件
        canvas = uiPanel.GetComponentInParent<Canvas>();
        uiPanel.SetActive(false); // 初始化时隐藏UI面板
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform)
                {
                    ShowUI(hit.transform.position);
                }
            }
        }
    }

    void ShowUI(Vector3 worldPosition)
    {
        // 将世界坐标转换为屏幕坐标
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
        
        // 设置UI面板的位置
        uiPanel.SetActive(true);
        RectTransform uiRectTransform = uiPanel.GetComponent<RectTransform>();
        uiRectTransform.position = screenPosition + new Vector2(0, -300); // 根据需要调整偏移
    }
}