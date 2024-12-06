using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShooterEnemy : Enemy
{
    public GameObject shooterEnemyBullet;
    public int damageAmount = 5; // 每次造成的伤害量
    public float bulletMoveSpeed = 2.0f; // 子弹移动时间
    
    
    protected override void OnEnable()
    {
        base.OnEnable();
        // moveSpeed = 2f;
        enemyType = EnemyType.ShooterEnemy;
    }
    
    private void Update()
    {
        Attack();
    }
    
    // 重写攻击方法
    public override void AttackMode(GameObject target)
    {
        Shoot(target);
    }

    private void Shoot(GameObject ant)
    {
        if (shooterEnemyBullet != null)
        {
            transform.rotation = Quaternion.LookRotation(ant.transform.position - transform.position);
            GameObject bullet = Instantiate(shooterEnemyBullet, transform.position, Quaternion.identity);
            BezierBulletWithPath bulletWithPath = bullet.GetComponent<BezierBulletWithPath>();
            bulletWithPath.targetEnemy = ant;
            bulletWithPath.endPoint = ant.transform.position;
            bulletWithPath.speed = bulletMoveSpeed;
            bulletWithPath.damageAmount = damageAmount;
            bulletWithPath.ShootBullet();
        }
    }
}
