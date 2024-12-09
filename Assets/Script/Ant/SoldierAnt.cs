using System.Collections;
using System.Linq;
using UnityEngine;

public class SoldierAnt : Ant
{
    // 攻击范围
    public float collectEnemyRange = 1f;
    public float minDistanceToOtherAnts = 0.5f; // 蚂蚁之间的最小距离
    public float damageInterval = 1.0f; // 每秒造成一次伤害
    public int attackPower = 5; // 每次造成的伤害量
    
    private int backCurrentWaypointIndex = 1;
    private bool isMovingToEnemy = false;
    // private GameObject pickedEnemy;

    private void Update()
    {
        PerformAction();
    }

    protected Coroutine moveToEnemyCoroutine;
    
    // 攻击行为
    protected override void PerformAction()
    {
        if (!isMovingToEnemy)
        {
            Enemy enemy = GetClosestEnemy();
            if (waypoint != null&&enemy!=null&&Vector3.Distance(transform.position, enemy.transform.position) < collectEnemyRange)
            {
                isMovingToEnemy = true;
                isPatrolPaused = true;
                moveToEnemyCoroutine = StartCoroutine(MoveToEnemy(enemy));
            }
        }
    }

    private IEnumerator MoveToEnemy(Enemy enemy)
    {
        backCurrentWaypointIndex = Mathf.Min(waypoint.pathList.Count - 1, currentWaypointIndex - 1);
        float damageTimer = damageInterval;
        
        while (enemy != null)
        {
            Vector3 enemyTransformPosition = enemy.transform.position;
            enemyTransformPosition.y = transform.position.y; // 忽略 y 轴
            
            Vector3 direction = (enemyTransformPosition - transform.position).normalized;
            float enemyRadius = enemy.GetComponent<Collider>().bounds.extents.magnitude; // 获取敌人的半径
            
            Vector3 adjustedTarget = enemyTransformPosition - direction * enemyRadius;
            // 移动和转向该目标点
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * patrolSpeed * 2);
            transform.position = Vector3.MoveTowards(transform.position, adjustedTarget, patrolSpeed * Time.deltaTime);


            // 检查是否达到目标点
            // if (Vector3.Distance(transform.position, adjustedTarget) < 0.1f)
            {
                while (enemy != null&&Vector3.Distance(transform.position, new Vector3(enemy.transform.position.x,transform.position.y,enemy.transform.position.z)- direction * enemyRadius) < 0.1f)
                {
                    damageTimer += Time.deltaTime;
                    if (damageTimer >= damageInterval&&enemy.health>0)
                    {
                        enemy.TakeDamage(attackPower); // 对敌人造成伤害
                        damageTimer = 0.0f; // 重置计时器
                    }
                    yield return null; // 等待下一帧
                }
                // yield break; // 结束协程
            }
            // StartCoroutine(HasEnemyBackAntColony());
            yield return null; // 等待下一帧
        }
        if(enemy == null)
        {
            isPatrolPaused = false;
            isMovingToEnemy = false;
            yield break;
        }
    }
    
    //按照路径返回巢穴
    private IEnumerator HasEnemyBackAntColony()
    {
        // StopCoroutine(moveToEnemyCoroutine);
        // 循环巡逻
        while (waypoint != null)
        {
            // 如果到达终点，开始返回
            if (waypoint != null)
            {
                // 计算前进方向并朝向该方向
                // Vector3 directionBack = (waypoint.pathList[waypoint.pathList.Count - 2] - transform.position).normalized;
                Vector3 directionBack = (waypoint.pathList[^2] - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(directionBack);
                backToNest = true;
                // 返回路径
                for (int i = backCurrentWaypointIndex; i >= 0; i--)
                {
                    while (waypoint != null && Vector3.Distance(transform.position, waypoint.pathList[i]) > 0.1f)
                    {
                        float speed = patrolSpeed;
                        // 检查与其他蚂蚁的距离
                        // 如果距离上一只蚂蚁太近，减慢速度
                        // if (previousAnt != null && previousAnt.backToNest == backToNest)
                        // {
                        //     float distanceToPreviousAnt = Vector3.Distance(transform.position, previousAnt.transform.position);
                        //     if (distanceToPreviousAnt < minDistanceToPreviousAnt)
                        //     {
                        //         speed *= Mathf.Max(Mathf.Clamp01(distanceToPreviousAnt / minDistanceToPreviousAnt),0.5f);
                        //     }
                        // }
                        
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

                if (waypoint != null)
                {
                    // 回收蚂蚁
                    // colony.foodCount += foodValue;
                    // AntColony.instance.ShowFoodCount();
                    colony.RecycleAnt(gameObject);
                    //删除pickedFood
                    // Destroy(pickedEnemy);
                    isMovingToEnemy = false; // 到达食物位置后重置标志
                    yield break; // 结束协程
                }
            }
        }
        
        while (waypoint == null)
        {
            // 计算前进方向并朝向该方向
            Vector3 direction = (colony.transform.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * patrolSpeed*2);
            transform.position = Vector3.MoveTowards(transform.position, colony.transform.position, Time.deltaTime * patrolSpeed);
            // 检查是否到达巢穴位置
            if (Vector3.Distance(transform.position, colony.transform.position) < 0.1f)
            {
                // 回收蚂蚁
                // colony.foodCount += foodValue;
                // AntColony.instance.ShowFoodCount();
                colony.RecycleAnt(gameObject);
                //删除pickedFood
                // Destroy(pickedEnemy);
                isMovingToEnemy = false; // 到达食物位置后重置标志
                yield break; // 结束协程
            }
            yield return null;
        }
    }
    
    //获取离得最近的敌人
    private Enemy GetClosestEnemy()
    {
        Enemy closestEnemy = null;
        float minDistance = float.MaxValue;
        foreach (var enemy in EnemyManager.Instance.enemyList)
        {
            if (enemy != null)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }
        return closestEnemy;
    }
}