using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Ant : MonoBehaviour, IDamageable
{
    public int price = 0; // 蚂蚁价格
    [HideInInspector]public AntTrack.AntPath waypoint; // 巡逻路径
    public AntColony.VariousAnt variousAnt; // 蚂蚁种类
    [HideInInspector]public int currentWaypointIndex = 1;
    [HideInInspector]public AntColony colony; // 指向 AntColony 的引用
    public float patrolSpeed = 1.0f; // 巡逻速度
    public float minDistanceToPreviousAnt = 1.0f; // 与上一只蚂蚁的最小距离
    public float slowDownFactor = 0.5f; // 减慢速度的因子
    public AntColony.AntType antType; // 蚂蚁种类
    public int health;
    //距离巢穴的距离过近时解除限制
    [HideInInspector]public float minDistanceToColony = 0.0f;
    
    [HideInInspector]public bool backToNest = false; // 是否返回巢穴
    protected Coroutine patrolCoroutine;

    protected virtual void OnEnable()
    {
        if (waypoint!=null&&waypoint.pathList.Count > 0)
        {
            currentWaypointIndex = 0;
            backToNest = false;
            // 计算前进方向并朝向该方向
            Vector3 direction = (waypoint.pathList[currentWaypointIndex] - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;

            patrolCoroutine = StartCoroutine(Patrol());
        }
    }

    protected Ant previousAnt;
    public bool isPatrolPaused = false;
    private IEnumerator Patrol()
    {
        // 循环巡逻
        while (waypoint!=null)
        {
            int pathListCount = waypoint.pathList.Count;
            if (pathListCount > 1)
            {
                transform.rotation = Quaternion.LookRotation((waypoint.pathList[1] - transform.position).normalized);
            }
            // 前往当前巡逻点
            if (waypoint != null && !backToNest)
            {
                for (int i = 1; i < pathListCount; i++)
                {
                    currentWaypointIndex++;
                    while (waypoint != null && i < waypoint.pathList.Count && Vector3.Distance(transform.position, waypoint.pathList[i]) > 0.1f)
                    {
                        if (isPatrolPaused)
                        {
                            yield return null; // 暂时暂停，等待下一帧重新检查
                            continue;
                        }

                        float speed = patrolSpeed;

                        // 获取上一只蚂蚁
                        previousAnt = GetPreviousAnt();
                        // 如果距离上一只蚂蚁太近，减慢速度
                        if (previousAnt != null && previousAnt.backToNest == backToNest)
                        {
                            float distanceToPreviousAnt =
                                Vector3.Distance(transform.position, previousAnt.transform.position);
                            if (distanceToPreviousAnt < minDistanceToPreviousAnt)
                            {
                                if (Vector3.Distance(transform.position, colony.transform.position)<minDistanceToColony)
                                {
                                    speed *= Mathf.Clamp01(distanceToPreviousAnt / minDistanceToPreviousAnt);
                                }
                                else
                                {
                                    speed *= Mathf.Max(Mathf.Clamp01(distanceToPreviousAnt / minDistanceToPreviousAnt), slowDownFactor);
                                }
                            }
                        }

                        // 计算前进方向并朝向该方向
                        Vector3 direction = (waypoint.pathList[i] - transform.position).normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation =
                            Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed * 2);

                        transform.position = Vector3.MoveTowards(transform.position, waypoint.pathList[i],
                            Time.deltaTime * speed);
                        yield return null; // 等待下一帧
                    }
                }
                backToNest = true;
            }
            // while (!backToNest&&waypoint!=null&&Vector3.Distance(transform.position, waypoint.pathList[currentWaypointIndex]) > 0.1f)
            // {
            //
            //     currentWaypointIndex++;
            //     if (currentWaypointIndex >= waypoint.pathList.Count)
            //     {
            //         backToNest = true;
            //         currentWaypointIndex--;
            //     }
            //     
            //     if (isPatrolPaused)
            //     {
            //         yield return null; // 暂时暂停，等待下一帧重新检查
            //         continue;
            //     }
            //     float speed = patrolSpeed;
            //
            //     // 如果距离上一只蚂蚁太近，减慢速度
            //     if (previousAnt != null && previousAnt.backToNest == backToNest)
            //     {
            //         float distanceToPreviousAnt = Vector3.Distance(transform.position, previousAnt.transform.position);
            //         if (distanceToPreviousAnt < minDistanceToPreviousAnt)
            //         {
            //             speed *= Mathf.Max(Mathf.Clamp01(distanceToPreviousAnt / minDistanceToPreviousAnt),0.5f);
            //         }
            //     }
            //     
            //     // 计算前进方向并朝向该方向
            //     Vector3 direction = (waypoint.pathList[currentWaypointIndex] - transform.position).normalized;
            //     Quaternion targetRotation = Quaternion.LookRotation(direction);
            //     transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed*2);
            //
            //     
            //     transform.position = Vector3.MoveTowards(transform.position, waypoint.pathList[currentWaypointIndex], Time.deltaTime * speed);
            //     yield return null; // 等待下一帧
            // }

            // 如果到达终点，开始返回
            if (waypoint!=null&&backToNest)
            {
                // 计算前进方向并朝向该方向
                // Vector3 directionBack = (waypoint.pathList[waypoint.pathList.Count - 2] - transform.position).normalized;
                Vector3 directionBack = (waypoint.pathList[^2] - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(directionBack);
                // 返回路径
                for (int i = pathListCount - 1; i >= 0; i--)
                {
                    currentWaypointIndex--;
                    while (waypoint!=null&& i < waypoint.pathList.Count&&Vector3.Distance(transform.position, waypoint.pathList[i]) > 0.1f)
                    {
                        if (isPatrolPaused)
                        {
                            yield return null; // 暂时暂停，等待下一帧重新检查
                            continue;
                        }
                        
                        float speed = patrolSpeed;

                        // 获取上一只蚂蚁
                        previousAnt = GetPreviousAnt();
                        // 如果距离上一只蚂蚁太近，减慢速度
                        if (previousAnt != null && previousAnt.backToNest == backToNest && Vector3.Distance(transform.position, colony.transform.position)>minDistanceToColony)
                        {
                            float distanceToPreviousAnt = Vector3.Distance(transform.position, previousAnt.transform.position);
                            if (distanceToPreviousAnt < minDistanceToPreviousAnt)
                            {
                                if (Vector3.Distance(transform.position, colony.transform.position)>minDistanceToColony)
                                {
                                    speed *= Mathf.Max(Mathf.Clamp01(distanceToPreviousAnt / minDistanceToPreviousAnt), slowDownFactor);
                                }
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

                if (waypoint != null)
                {
                    // 回收蚂蚁
                    colony.RecycleAnt(gameObject);
                    yield break; // 结束协程
                }
            }
        }

        while (waypoint == null)
        {
            if (isPatrolPaused)
            {
                yield return null; // 暂时暂停，等待下一帧重新检查
                continue;
            }
            // 计算前进方向并朝向该方向
            Vector3 direction = (colony.transform.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * patrolSpeed*2);
            transform.position = Vector3.MoveTowards(transform.position, colony.transform.position, Time.deltaTime * patrolSpeed);
            // 检查是否到达巢穴位置
            if (Vector3.Distance(transform.position, colony.transform.position) < 0.1f)
            {
                // 回收蚂蚁
                colony.RecycleAnt(gameObject);
                yield break; // 结束协程
            }
            yield return null;
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

    public virtual void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    private bool isDead = false;
    public virtual void Die()
    {
        if (!isDead)
        {
            isDead = true;
        // 默认的死亡行为，可以在子类中重写
        variousAnt.ants.Remove(this.gameObject);
        if (waypoint != null)
        {
            waypoint.ants.Remove(this); // 从路径上移除蚂蚁
            waypoint = null; // 清空蚂蚁的路径
        }
        UIManager.Instance.ShowAntCount(variousAnt, variousAnt.antPrefab.antPrefab.GetComponent<Ant>().antType);
        // AntColony.instance.DeleteAnt(antType);
        Destroy(gameObject);            
        }
    }
}