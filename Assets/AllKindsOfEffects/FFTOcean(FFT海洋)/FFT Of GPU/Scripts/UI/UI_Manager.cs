using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Manager : MonoBehaviour
{
    public FFTOcean fftOcean;
    // 引用滚动条对象
    public Scrollbar WindScale_scrollbar;
    public Scrollbar HeightScale_scrollbar;
    public Scrollbar TimeScale_scrollbar;
    public Scrollbar BubblesScale_scrollbar;
    
    public Camera camera1;
    public Camera camera2;
    void Start()
    {
        // 检查是否正确引用了滚动条对象
        if (WindScale_scrollbar != null)
        {
            WindScale_scrollbar.onValueChanged.AddListener(Set_WindScale);
        }
        if (HeightScale_scrollbar != null)
        {
            HeightScale_scrollbar.onValueChanged.AddListener(Set_HeightScale);
        }
        if (TimeScale_scrollbar != null)
        {
            TimeScale_scrollbar.onValueChanged.AddListener(Set_TimeScale);
        }
        if (BubblesScale_scrollbar != null)
        {
            BubblesScale_scrollbar.onValueChanged.AddListener(Set_BubblesScale);
        }
        
        // 初始时，启用camera1并禁用camera2
        camera1.enabled = true;
        camera2.enabled = false;
    }
    
    // 定义滚动条值变化时的处理函数
    void Set_WindScale(float value)
    {
        fftOcean.WindScale = value*30+1;
    }
    
    void Set_HeightScale(float value)
    {
        fftOcean.HeightScale = value*30+1;
    }
    
    void Set_TimeScale(float value)
    {
        fftOcean.TimeScale = value*10+1;
    }
    
    void Set_BubblesScale(float value)
    {
        fftOcean.BubblesScale = value*30+1;
    }
    
    public void SwitchCamera()
    {
        // 如果camera1当前是启用的，那么禁用它并启用camera2
        if (camera1.enabled)
        {
            camera1.enabled = false;
            camera2.enabled = true;
        }
        // 如果camera2当前是启用的，那么禁用它并启用camera1
        else
        {
            camera2.enabled = false;
            camera1.enabled = true;
        }
    }
}
