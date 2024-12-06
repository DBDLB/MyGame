using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AggressiveBeetle : Enemy
{
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
}
