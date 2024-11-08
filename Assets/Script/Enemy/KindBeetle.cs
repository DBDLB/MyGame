using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class KindBeetle : Enemy
{
    private Vector3 currentTarget; // 当前移动目标点
    private bool isMoving = false; // 标记是否在移动
    private Quaternion targetRotation; // 目标旋转

    private void Update()
    {
        Move(currentTarget, 2f);
    }

    public override void Move(Vector3 targetPosition, float speed)
    {
        if (!isMoving)
        {
            // 随机选择 30 度范围内的方向
            float randomAngle = Random.Range(-15f, 15f); // -60 到 60 度范围
            Quaternion rotation = Quaternion.Euler(0, randomAngle, 0); // 绕 y 轴旋转
            Vector3 direction = rotation * transform.forward; // 获取旋转后的方向

            // 选择该方向上一个随机距离的点
            float randomDistance = Random.Range(2f, 5f);
            Vector3 potentialTarget = transform.position + direction * randomDistance;

            // 检查是否在边界内，如果不在则 180 度转向
            if (!IsWithinBounds(potentialTarget))
            {
                // 转向180度，重新计算目标点
                direction = -direction;
                potentialTarget = transform.position + direction * randomDistance;
            }

            // 设置新的移动目标和旋转方向
            currentTarget = potentialTarget;
            targetRotation = Quaternion.LookRotation(direction);
            isMoving = true;
        }

        // 旋转和移动到目标点
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed * 4);
        transform.position = Vector3.MoveTowards(transform.position, currentTarget, speed * Time.deltaTime);

        // 如果到达目标点，重新选择新的方向和目标点
        if (Vector3.Distance(transform.position, currentTarget) < 0.1f)
        {
            isMoving = false;
        }
    }

    private bool IsWithinBounds(Vector3 targetPosition)
    {
        // 获取 BoxCollider 边界
        Bounds bounds = CameraController.Instance.groundCollider.bounds;

        // 检查目标点是否在边界内
        return bounds.Contains(targetPosition);
    }

    // 重写受伤方法
    public override void TakeDamage(int damage)
    {
        throw new System.NotImplementedException();
    }

    // 重写攻击方法
    public override void Attack()
    {
        throw new System.NotImplementedException();
    }

    // 重写死亡方法
    protected override void Die()
    {
        throw new System.NotImplementedException();
    }
}
