using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Ant : MonoBehaviour
{
    public List<Vector3> waypoints; // 巡逻路径
    public int currentWaypointIndex = 1;
    public AntColony colony; // 指向 AntColony 的引用
    public float patrolSpeed = 1.0f; // 巡逻速度

    protected virtual void OnEnable()
    {
        if (waypoints.Count > 0)
        {
            currentWaypointIndex = 1;
            StartCoroutine(Patrol());
        }
    }

    private IEnumerator Patrol()
    {
        // 循环巡逻
        while (true)
        {
            // 前往当前巡逻点
            while (Vector3.Distance(transform.position, waypoints[currentWaypointIndex]) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, waypoints[currentWaypointIndex], Time.deltaTime * patrolSpeed);
                yield return null; // 等待下一帧
            }

            // 更新到下一个巡逻点
            currentWaypointIndex++;

            // 如果到达终点，开始返回
            if (currentWaypointIndex >= waypoints.Count)
            {
                // 返回路径
                for (int i = waypoints.Count - 1; i >= 0; i--)
                {
                    while (Vector3.Distance(transform.position, waypoints[i]) > 0.1f)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, waypoints[i], Time.deltaTime * patrolSpeed);
                        yield return null; // 等待下一帧
                    }
                }

                // 回收蚂蚁
                colony.RecycleAnt(gameObject);
                yield break; // 结束协程
            }
        }
    }

    // 抽象方法，供子类实现
    protected abstract void PerformAction();
}