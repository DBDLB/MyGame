using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class Enemy : MonoBehaviour, IDamageable
{
    public enum EnemyType
    {
        KindBeetle,
        // 添加其他敌人类型
        AggressiveBeetle,
        FlyingBeetle,
        // 其他类型
    }
    protected virtual void OnEnable()
    {
        // 将敌人添加到 EnemyManager 的敌人列表中
        EnemyManager.Instance.enemyList.Add(this);
        StartCoroutine(Move());
    }
    
    public EnemyType enemyType; // 敌人类型
    public float moveSpeed = 0f; // 移动速度
    // 敌人的生命值
    public int health;
    // 敌人的攻击力
    public int attackPower;
    
    public bool isMovePaused = false;
    public bool isMovingToEnemy = false;
    public float damageInterval = 1.0f; // 每秒造成一次伤害
    public float collectEnemyRange = 1f; // 攻击范围

    // 处理敌人受到伤害的行为
    public virtual void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        // 默认的死亡行为，可以在子类中重写
        EnemyManager.Instance.enemyList.Remove(this);
        Destroy(gameObject);
    }

    // 抽象方法：处理敌人攻击的行为
    public virtual void Attack()
    {
        if (!isMovingToEnemy)
        {
            List<GameObject> antListAndFood = new List<GameObject>();
            foreach (var variousAnt in AntColony.variousAnts)
            {
                antListAndFood.AddRange(variousAnt.ants);
            }
        
            foreach (var food in FoodManager.Instance.foodList)
            {
                antListAndFood.Add(food.gameObject);
            }
            foreach (var target in antListAndFood)
            {
                if (target != null&&target.activeSelf)
                {
                    if (Vector3.Distance(transform.position, target.transform.position) < collectEnemyRange)
                    {
                        isMovingToEnemy = true;
                        isMovePaused = true;
                        StartCoroutine(MoveToTarget(target));
                        break;
                    }
                }
            }
        }
    }
    private IEnumerator MoveToTarget(GameObject enemy)
    {
        float damageTimer = damageInterval;
        float toTargetMoveSpeed = moveSpeed + 1;
        while (enemy != null && enemy.activeSelf)
        {
            Vector3 enemyTransformPosition = enemy.transform.position;
            enemyTransformPosition.y = transform.position.y; // 忽略 y 轴
            
            Vector3 direction = (enemyTransformPosition - transform.position).normalized;
            float enemyRadius = enemy.GetComponent<Collider>().bounds.extents.magnitude; // 获取敌人的半径
            
            Vector3 adjustedTarget = enemyTransformPosition - direction * enemyRadius;
            // 移动和转向该目标点
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * toTargetMoveSpeed * 2);
            transform.position = Vector3.MoveTowards(transform.position, adjustedTarget, toTargetMoveSpeed * Time.deltaTime);


            // 检查是否达到目标点
            // if (Vector3.Distance(transform.position, adjustedTarget) < 0.1f)
            {
                while (enemy != null&& enemy.activeSelf&&Vector3.Distance(transform.position, new Vector3(enemy.transform.position.x,transform.position.y,enemy.transform.position.z)- direction * enemyRadius) < 1f)
                {
                    damageTimer += Time.deltaTime;
                    if (damageTimer >= damageInterval)
                    {
                        enemy.GetComponent<IDamageable>().TakeDamage(attackPower); // 对敌人造成伤害
                        damageTimer = 0.0f; // 重置计时器
                    }
                    yield return null; // 等待下一帧
                }
                // yield break; // 结束协程
            }
            // StartCoroutine(HasEnemyBackAntColony());
            yield return null; // 等待下一帧
        }
        if(enemy == null||!enemy.activeSelf)
        {
            isMovePaused = false;
            isMovingToEnemy = false;
            currentTarget = transform.position;
            yield break;
        }
    }

    // // 虚方法：处理敌人死亡的行为
    // protected virtual void Die()
    // {
    //     // 默认的死亡行为，可以在子类中重写
    //     EnemyManager.Instance.enemyList.Remove(this);
    //     Destroy(gameObject);
    // }
    
    // 移动方法：移动敌人到目标位置
    Vector3 currentTarget;
    protected virtual IEnumerator Move()
    {
        Quaternion targetRotation;
        while (true)
        {
            if (isMovePaused)
            {
                yield return null; // 暂时暂停，等待下一帧重新检查
                continue;
            }
            // 随机选择 30 度范围内的方向
            float randomAngle = Random.Range(-30f, 30f); // -60 到 60 度范围
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

            while (Vector3.Distance(transform.position, currentTarget) >= 0.1f)
            {
                if (isMovePaused)
                {
                    yield return null; // 暂时暂停，等待下一帧重新检查
                    continue;
                }
                // 旋转和移动到目标点
                transform.rotation =
                    Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed * 4);
                transform.position = Vector3.MoveTowards(transform.position, currentTarget, moveSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }
    private bool IsWithinBounds(Vector3 targetPosition)
    {
        // 获取 BoxCollider 边界
        Bounds bounds = CameraController.Instance.groundCollider.bounds;

        // 创建一个新的 Vector3 变量，只保留 x 和 z 轴的值，y 轴设为边界的中心 y 值
        Vector3 targetXZ = new Vector3(targetPosition.x, bounds.center.y, targetPosition.z);

        // 检查目标点是否在边界内
        return bounds.Contains(targetXZ);
    }
}