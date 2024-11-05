using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject[] panels; // 存储所有面板的数组

    private void Start()
    {
        // 初始化时隐藏所有面板
        HideAllPanels();
    }

    // 显示指定面板
    public void ShowPanel(string panelName)
    {
        HideAllPanels(); // 隐藏其他面板

        foreach (GameObject panel in panels)
        {
            if (panel.name == panelName)
            {
                panel.SetActive(true);
                break;
            }
        }
    }

    // 隐藏所有面板
    public void HideAllPanels()
    {
        foreach (GameObject panel in panels)
        {
            panel.SetActive(false);
        }
    }

    // 切换面板显示状态
    public void TogglePanel(string panelName)
    {
        foreach (GameObject panel in panels)
        {
            if (panel.name == panelName)
            {
                panel.SetActive(!panel.activeSelf);
                break;
            }
        }
    }
}