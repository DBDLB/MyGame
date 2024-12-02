using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    
    public static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIManager>();
                if (instance == null)
                {
                    GameObject singleton = new GameObject(typeof(UIManager).Name);
                    instance = singleton.AddComponent<UIManager>();
                }
            }
            return instance;
        }
    }
    
    public GameObject[] panels; // 存储所有面板的数组
    public TextMeshProUGUI FoodShowText; // 显示食物数量的文本
    public ShowAntNumText[] showAntNumTexts; // 显示蚂蚁数量的文本
    
    [System.Serializable]
    public class ShowAntNumText
    {
        public AntColony.AntType antType;
        public TextMeshProUGUI antNumText;
    }


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
    
    // 显示食物数量
    public void ShowFoodCount()
    {
        FoodShowText.text = "食物数量：" + AntColony.instance.foodCount;
    }
    
    // 显示蚂蚁数量
    public void ShowAntCount(AntColony.VariousAnt variousAnt,AntColony.AntType antType)
    {
        foreach (ShowAntNumText showAntNumText in showAntNumTexts)
        {
            if (showAntNumText.antType == antType)
            {
                switch (antType)
                {
                    case AntColony.AntType.WorkerAnt:
                        showAntNumText.antNumText.text = "工蚁：" + variousAnt.ants.Count;
                        break;
                    case AntColony.AntType.SoldierAnt:
                        showAntNumText.antNumText.text = "兵蚁：" + variousAnt.ants.Count;
                        break;
                }
            }
        }
    }
}