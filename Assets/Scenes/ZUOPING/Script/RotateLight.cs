using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateLight : MonoBehaviour
{
    //旋转主光源Y轴
    public float rotateSpeed = 1.0f;
    public GameObject lightObj;
    
    //记录原始旋转角度
    private Vector3 originalRotation;
    
    bool isRotate = false;
    
    // 当物体进入Box Collider时开始旋转
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            //记录原始旋转角度
            originalRotation = lightObj.transform.eulerAngles;
            isRotate = true;
        }
    }
    
    // 当物体离开Box Collider时停止旋转
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            isRotate = false;
            lightObj.transform.eulerAngles = originalRotation;
        }
    }
    
    void Update()
    {
        if (isRotate)
        {
            lightObj.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }
    }
}
