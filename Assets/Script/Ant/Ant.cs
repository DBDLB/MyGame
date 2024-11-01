using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Ant : MonoBehaviour
{
    public AntTrack.AntPath waypoint; // 巡逻路径
    public AntColony.VariousAnt variousAnt; // 蚂蚁种类
    public int currentWaypointIndex = 1;
    public AntColony colony; // 指向 AntColony 的引用
    public float patrolSpeed = 1.0f; // 巡逻速度
    public float minDistanceToPreviousAnt = 1.0f; // 与上一只蚂蚁的最小距离
    public float slowDownFactor = 0.5f; // 减慢速度的因子
    
    //距离巢穴的距离过近时解除限制
    public float minDistanceToColony = 2.0f;
    
    public bool backToNest = false; // 是否返回巢穴

    protected virtual void OnEnable()
    {
        if (waypoint.pathList.Count > 0)
        {
            currentWaypointIndex = 1;
            // 计算前进方向并朝向该方向
            Vector3 direction = (waypoint.pathList[currentWaypointIndex] - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;

            StartCoroutine(Patrol());
        }
    }

    private IEnumerator Patrol()
    {
        // 循环巡逻
        while (true)
        {
            // 获取上一只蚂蚁
            Ant previousAnt = GetPreviousAnt();
            // 前往当前巡逻点
            while (Vector3.Distance(transform.position, waypoint.pathList[currentWaypointIndex]) > 0.1f)
            {
                backToNest = false;
                float speed = patrolSpeed;

                // 如果距离上一只蚂蚁太近，减慢速度
                if (previousAnt != null && previousAnt.backToNest == backToNest)
                {
                    float distanceToPreviousAnt = Vector3.Distance(transform.position, previousAnt.transform.position);
                    if (distanceToPreviousAnt < minDistanceToPreviousAnt)
                    {
                        speed *= Mathf.Clamp01(distanceToPreviousAnt / minDistanceToPreviousAnt);
                    }
                }
                
                // 计算前进方向并朝向该方向
                Vector3 direction = (waypoint.pathList[currentWaypointIndex] - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed*2);

                
                transform.position = Vector3.MoveTowards(transform.position, waypoint.pathList[currentWaypointIndex], Time.deltaTime * speed);
                yield return null; // 等待下一帧
            }

            // 更新到下一个巡逻点
            currentWaypointIndex++;

            // 如果到达终点，开始返回
            if (currentWaypointIndex >= waypoint.pathList.Count)
            {
                // 计算前进方向并朝向该方向
                // Vector3 directionBack = (waypoint.pathList[waypoint.pathList.Count - 2] - transform.position).normalized;
                Vector3 directionBack = (waypoint.pathList[^2] - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(directionBack);
                backToNest = true;
                // 返回路径
                for (int i = waypoint.pathList.Count - 1; i >= 0; i--)
                {
                    while (Vector3.Distance(transform.position, waypoint.pathList[i]) > 0.1f)
                    {
                        float speed = patrolSpeed;

                        // 如果距离上一只蚂蚁太近，减慢速度
                        if (previousAnt != null && previousAnt.backToNest == backToNest && Vector3.Distance(transform.position, colony.transform.position)>minDistanceToColony)
                        {
                            float distanceToPreviousAnt = Vector3.Distance(transform.position, previousAnt.transform.position);
                            if (distanceToPreviousAnt < minDistanceToPreviousAnt)
                            {
                                speed *= Mathf.Clamp01(distanceToPreviousAnt / minDistanceToPreviousAnt);
                            }
                        }
                        
                        // 计算前进方向并朝向该方向
                        Vector3 direction = (waypoint.pathList[i] - transform.position).normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed*2);
                        
                        transform.position = Vector3.MoveTowards(transform.position, waypoint.pathList[i], Time.deltaTime * speed);
                        yield return null; // 等待下一帧
                    }
                }

                // 回收蚂蚁
                colony.RecycleAnt(gameObject);
                yield break; // 结束协程
            }
        }
    }

    // 获取上一只蚂蚁的方法
    private Ant GetPreviousAnt()
    {
        // 假设蚂蚁在同一个路径上的顺序是固定的
        int previousAntIndex = waypoint.ants.IndexOf(this) - 1;
        if (previousAntIndex >= 0)
        {
            return waypoint.ants[previousAntIndex];
        }
        return null;
    }
    
    // 抽象方法，供子类实现
    protected abstract void PerformAction();
}