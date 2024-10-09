using System;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public float health = 100f;             // 敌人的生命值
    public float speed = 3f;                // 敌人的移动速度
    public float attackPower = 10f;         // 敌人的攻击力

    protected Vector3 playerPosition;             // 玩家对象的变换
    
    public event Action OnDeath; 

    protected virtual void Start()
    {
        playerPosition = GameManager.Instance.playerPosition;
    }

    protected virtual void Update()
    {
        MoveTowardsPlayer();
        AttackPlayer();
    }

    // 敌人接收到伤害
    public virtual void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    // 敌人的死亡处理
    protected virtual void Die()
    {
        // 调用死亡事件
        OnDeath?.Invoke();
        // 处理死亡逻辑，比如播放动画、掉落物品等
        Destroy(gameObject);
    }

    // 敌人朝向玩家移动
    protected virtual void MoveTowardsPlayer()
    {
        playerPosition = GameManager.Instance.playerPosition;
        Vector3 direction = (playerPosition - transform.position).normalized;
        direction.y = 0;
        transform.position += direction * speed * Time.deltaTime;
    }

    // 敌人攻击玩家（可以被具体类实现）
    protected abstract void AttackPlayer();
}