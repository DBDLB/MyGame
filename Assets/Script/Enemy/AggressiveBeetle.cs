using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class AggressiveBeetle : Enemy
{
    public GameObject aggressiveBeetle;
    
    
    protected override void OnEnable()
    {
        base.OnEnable();
        enemyType = EnemyType.AggressiveBeetle;
        isMovingToEnemy = false;
    }

    // 重写受伤方法
    // public override void TakeDamage(int damage)
    // {
    //     throw new System.NotImplementedException();
    // }

    // 重写攻击方法
    public override void Attack()
    {
        base.Attack();
    }

    private void Update()
    {
        Attack();
    }

    // 重写死亡方法
    public override void Die()
    {
        GameObject Carcass = Instantiate(aggressiveBeetle);
        Carcass.transform.position = transform.position;
        Carcass.transform.rotation = Quaternion.Euler(0, 0, 180);
        base.Die();
    }
}
