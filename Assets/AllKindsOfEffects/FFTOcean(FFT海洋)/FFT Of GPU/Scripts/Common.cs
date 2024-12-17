using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用功能
/// </summary>
public static class Common
{
    static public GameObject character;


    //该函数接受一个父物体作为参数，并在其子物体中搜索具有特定组件（通过泛型约束 T）的物体。
    //如果在父物体或其子物体中找到了该组件，则函数返回该物体的 Transform。
    static int count;

    // public static Transform FindChild<T>(Transform parent) where T : Component
    // {
    //     Transform childTF;
    //     childTF = parent.GetComponentInChildren<HealthBar>().transform;
    //     if (childTF != null)
    //         return childTF;
    //     count = parent.childCount;
    //     for (int i = 0; i < count; i++)
    //     {
    //         childTF = FindChild<HealthBar>(parent.GetChild(i));
    //         if (childTF != null)
    //             return childTF;
    //     }
    //     return null;
    // }

    public static Transform FindChildByName(Transform parentObject, string childName)
    {
        //在子物体中查找
        Transform childTf = parentObject.Find(childName);
        if (childTf != null)
            return childTf;

        for (int i = 0; i < parentObject.childCount; i++)
        {
            //将任务移交给子物体
            childTf = FindChildByName(parentObject.GetChild(i), childName);
            if (childTf != null) return childTf;
        }
        return null;
    }
}
