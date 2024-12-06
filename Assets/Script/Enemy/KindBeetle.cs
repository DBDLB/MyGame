using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class KindBeetle : Enemy
{
    protected override void OnEnable()
    {
        base.OnEnable();
        // moveSpeed = 2f;
        enemyType = EnemyType.KindBeetle;
    }

    // 重写受伤方法
    // public override void TakeDamage(int damage)
    // {
    //     throw new System.NotImplementedException();
    // }

    // 重写攻击方法
    public override void Attack()
    {
        // throw new System.NotImplementedException();
    }
}
