using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    // 敌人的生命值
    public int health;

    // 抽象方法：处理敌人受到伤害的行为
    public abstract void TakeDamage(int damage);

    // 抽象方法：处理敌人攻击的行为
    public abstract void Attack();

    // 虚方法：处理敌人死亡的行为
    protected virtual void Die()
    {
        // 默认的死亡行为，可以在子类中重写
        Destroy(gameObject);
    }
    
    // 移动方法：移动敌人到目标位置
    public abstract void Move(Vector3 targetPosition, float speed);
}