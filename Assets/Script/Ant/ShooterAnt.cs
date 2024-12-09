using UnityEngine;
using System.Collections;
using System.Linq;

public class ShooterAnt : Ant
{
    //收集食物范围
    private int backCurrentWaypointIndex = 1;
    public GameObject shooterAntBullet;
    //攻击范围
    public float shootRange = 10f;
    public int attackPower = 5; // 每次造成的伤害量
    public float shootInterval = 1.0f;
    public float bulletMoveSpeed = 2.0f; // 子弹移动时间

    private void Update()
    {
        PerformAction();
    }

    //获取离得最近的敌人
    private Enemy GetClosestEnemy(out float distance)
    {
        if (EnemyManager.Instance.enemyList.Count == 0)
        {
            distance = 0;
            return null;
        }
        else
        {
            Enemy closestEnemy = EnemyManager.Instance.enemyList.FirstOrDefault();
            float closestDistance = Vector3.Distance(transform.position, closestEnemy.transform.position);
            foreach (Enemy enemy in EnemyManager.Instance.enemyList)
            {
                float tempDistance = Vector3.Distance(transform.position, enemy.transform.position);
                if (tempDistance < closestDistance)
                {
                    closestDistance = tempDistance;
                    closestEnemy = enemy;
                }
            }
            distance = closestDistance;
            return closestEnemy;
        }
    }

    private float shootTimer;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        shootTimer = shootInterval;
    }
    
    protected override void PerformAction()
    {
        shootTimer += Time.deltaTime;
        if (shootTimer >= shootInterval)
        {
            shootTimer = 0f;
            float distance = 0f;
            Enemy enemy = GetClosestEnemy(out distance);
            if (enemy != null)
            {
                if (distance < shootRange)
                {
                    isPatrolPaused = true;
                    Shoot(enemy);
                }
                else
                {
                    isPatrolPaused = false;
                    shootTimer = shootInterval;
                }
            }
            else
            {
                isPatrolPaused = false;
                shootTimer = shootInterval;
            }
            
        }
    }
    
    private void Shoot(Enemy enemy)
    {
        if (shooterAntBullet != null)
        {
            transform.rotation = Quaternion.LookRotation(enemy.transform.position - transform.position);
            GameObject bullet = Instantiate(shooterAntBullet, transform.position, Quaternion.identity);
            BezierBulletWithPath bulletWithPath = bullet.GetComponent<BezierBulletWithPath>();
            bulletWithPath.targetEnemy = enemy.gameObject;
            bulletWithPath.endPoint = enemy.transform.position;
            bulletWithPath.speed = bulletMoveSpeed;
            bulletWithPath.attackPower = attackPower;
            bulletWithPath.ShootBullet();
        }
    }

    public override void Die()
    {
        // this.StopAllCoroutines();
        // if (pickedFood != null)
        // {
        //     InsectCarcass insectCarcass = pickedFood.GetComponent<InsectCarcass>();
        //     if(insectCarcass != null)
        //     {
        //         pickedFood.transform.localRotation = Quaternion.Euler(0, 0,  0);
        //         pickedFood.transform.SetParent(null, true);
        //         insectCarcass.ReleaseAnt();
        //     }
        // }

        // ReleaseAnt();
        base.Die();
    }
    
}