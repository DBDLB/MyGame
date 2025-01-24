using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingAntBullet : MonoBehaviour
{
    public Vector3[] path;            // 预计算的贝塞尔曲线路径
    public float speed = 5.0f;        // 子弹移动速度
    public int attackPower = 5; // 每次造成的伤害量
    public float attackRange = 2.0f; // 攻击范围

    private int currentIndex = 0;     // 当前路径点索引
    private float distanceCovered = 0.0f; // 累计移动距离

    public Vector3 endPoint = new Vector3(0, 0, 10);
    public void ShootBullet()
    {
        path = CreateBezierCurve.GetBezierPath(transform.position, endPoint, Random.Range(30, 65), 10);
        if (path == null || path.Length == 0)
        {
            Debug.LogError("Path is not defined!");
            return;
        }
        currentIndex = 0; // 初始化索引
        distanceCovered = 0.0f; // 初始化移动距离
    
        // 初始化位置为路径的起点
        // transform.position = path[0];
    }

    void Update()
    {
        if (path == null || path.Length <= 1)
            return;

        // 计算当前段的长度
        float segmentLength = Vector3.Distance(path[currentIndex], path[currentIndex + 1]);

        // 计算当前段的移动距离
        distanceCovered += speed * Time.deltaTime;

        // 计算在路径上的位置
        float t = distanceCovered / segmentLength;

        // 移动到当前路径点与下一个路径点之间的位置
        transform.position = Vector3.Lerp(path[currentIndex], path[currentIndex + 1], t);

        // 如果已经到达当前段的终点，则更新索引
        if (t >= 1.0f && currentIndex < path.Length - 2)
        {
            currentIndex++;
            distanceCovered = 0.0f; // 重置移动距离
        }

        // 检查是否到达终点
        if (currentIndex >= path.Length - 2 && t >= 1.0f)
        {
            OnReachEnd();
        }
    }
    
    /// <summary>
    /// 当子弹到达目标位置时的回调
    /// </summary>
    private void OnReachEnd()
    {
        
        List<GameObject> enemiesToDamage = new List<GameObject>();

        foreach (var enemy in EnemyManager.Instance.enemyList)
        {
            if (Vector3.Distance(transform.position, enemy.transform.position) < attackRange)
            {
                enemiesToDamage.Add(enemy.gameObject);
            }
        }

        foreach (var enemy in enemiesToDamage)
        {
            enemy.GetComponent<IDamageable>().TakeDamage(attackPower);
        }

        Destroy(gameObject); // 删除子弹
    }
}
