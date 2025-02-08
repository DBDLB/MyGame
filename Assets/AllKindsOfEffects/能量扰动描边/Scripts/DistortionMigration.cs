using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistortionMigration : MonoBehaviour
{
    public float decayFactor = 0.95f;      // 衰减因子
    public float jitterThreshold = 0.01f;    // 抖动阈值
    private Vector3 previousPosition;
    public Material material;
    public Vector3 offset;

    // Start 初始化
    void Start()
    {
        previousPosition = transform.position;
    }

    // FixedUpdate 每帧执行（适合处理物理运动）
    void FixedUpdate()
    {
        // 计算本帧位移
        Vector3 movementDelta = transform.position - previousPosition;
        previousPosition = transform.position;

        // 仅当移动距离超过阈值时累加偏移
        if (movementDelta.magnitude > jitterThreshold)
        {
            // 根据需求可以选择是否归一化 movementDelta
            // 例如：offset += movementDelta.normalized;
            offset += movementDelta;
        }

        // 对已有偏移进行衰减
        offset *= decayFactor;

        // offset = new Vector3(Mathf.Min(offset.x,1), offset.y, Mathf.Abs(offset.z));
        // 始终更新 Shader 中的偏移量
        material.SetVector("DistortOffset", offset);
    }
}