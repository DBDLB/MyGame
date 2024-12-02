using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Food : MonoBehaviour,IDamageable
{
    // 食物的价值
    public int foodValue = 1;
    public int accommodateAntCount = 1;
    public Ant[] hasAnt;
    
    // 抽象方法，供子类实现：处理蚂蚁拾取食物后的行为
    public abstract GameObject OnFoodPicked(WorkerAnt ant);
    
    protected virtual void OnEnable()
    {
        // 将食物添加到 FoodManager 的食物列表中
        FoodManager.Instance.foodList.Add(this);
        hasAnt = new Ant[accommodateAntCount];
    }
    
    public virtual void AddAnt(Ant ant)
    {
        for (int i = 0; i < hasAnt.Length; i++)
        {
            if (hasAnt[i] == null)
            {
                hasAnt[i] = ant;
                break;
            }
        }
    }
    // protected virtual void OnDestroy()
    // {
    //     // 将食物从 FoodManager 的食物列表中移除
    //     FoodManager.Instance.foodList.Remove(this);
    // }

    public virtual void TakeDamage(int damage)
    {
        foodValue -= damage;
        if (foodValue <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        // 默认的死亡行为，可以在子类中重写
        FoodManager.Instance.foodList.Remove(this);
        Destroy(gameObject);
    }
}
