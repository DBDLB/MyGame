using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Manager_ComputeShader : MonoBehaviour
{
    //笔刷设置绘制元素
    public void DrawMode(int mouseMode)
    {
        ComputeTexFlow.mouseMode = mouseMode;
    }

    //笔刷设置大小
    public void SetBrushSize(int size)
    {
        ComputeTexFlow.brushSize = size*2;
    }
}
