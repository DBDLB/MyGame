using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
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
    }
    
    public EnemyType enemyType; // 敌人类型
    // 敌人的生命值
    public int health;
    // 敌人的攻击力
    public int attackPower;

    // 抽象方法：处理敌人受到伤害的行为
    public virtual void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    // 抽象方法：处理敌人攻击的行为
    public abstract void Attack();

    // 虚方法：处理敌人死亡的行为
    protected virtual void Die()
    {
        // 默认的死亡行为，可以在子类中重写
        EnemyManager.Instance.enemyList.Remove(this);
        Destroy(gameObject);
    }
    
    // 移动方法：移动敌人到目标位置
    public abstract void Move(Vector3 targetPosition, float speed);
}