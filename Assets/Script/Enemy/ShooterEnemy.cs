using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShooterEnemy : Enemy
{
    public GameObject shooterEnemyBullet;
    public float bulletMoveSpeed = 2.0f; // 子弹移动时间
    
    
    protected override void OnEnable()
    {
        shootTimer = shootInterval;
        base.OnEnable();
        // moveSpeed = 2f;
        enemyType = EnemyType.ShooterEnemy;
    }
    
    private void Update()
    {
        Attack();
    }
    
    private float shootTimer;
    public float shootInterval = 1.0f;
    // 重写攻击方法
    public override void Attack()
    {
        if (!isMovingToEnemy)
        {
            float distance = 0f;
            GameObject antOrFood = GetClosestAntOrFood(out distance);
            if (antOrFood != null && antOrFood.activeSelf)
            {
                if (Vector3.Distance(transform.position, antOrFood.transform.position) < collectEnemyRange)
                {
                    isMovingToEnemy = true;
                    isMovePaused = true;
                    AttackMode(antOrFood);
                }
            }
        }
    }

    //获取离得最近的蚂蚁或食物
    private GameObject GetClosestAntOrFood(out float distance)
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
        
        if (antListAndFood.Count == 0)
        {
            distance = 0;
            return null;
        }
        else
        {
            GameObject closestEnemy = antListAndFood.FirstOrDefault();
            float closestDistance = Vector3.Distance(transform.position, closestEnemy.transform.position);
            foreach (GameObject antOrFood in antListAndFood)
            {
                float tempDistance = Vector3.Distance(transform.position, antOrFood.transform.position);
                if (tempDistance < closestDistance)
                {
                    closestDistance = tempDistance;
                    closestEnemy = antOrFood;
                }
            }
            distance = closestDistance;
            return closestEnemy;
        }
    }
    
    public override void AttackMode(GameObject target)
    {
        StartCoroutine(Shoot(target));
    }

    private IEnumerator Shoot(GameObject ant)
    {
        float distance = Vector3.Distance(transform.position, ant.transform.position);
        while (distance < collectEnemyRange && ant != null && ant.activeSelf)
        {
            distance = Vector3.Distance(transform.position, ant.transform.position);
            shootTimer += Time.deltaTime;
            if (shootTimer >= shootInterval)
            {
                if (shooterEnemyBullet != null && ant != null && ant.activeSelf)
                {
                    transform.rotation = Quaternion.LookRotation(ant.transform.position - transform.position);
                    GameObject bullet = Instantiate(shooterEnemyBullet, transform.position, Quaternion.identity);
                    BezierBulletWithPath bulletWithPath = bullet.GetComponent<BezierBulletWithPath>();
                    bulletWithPath.targetEnemy = ant;
                    bulletWithPath.endPoint = ant.transform.position;
                    bulletWithPath.speed = bulletMoveSpeed;
                    bulletWithPath.attackPower = attackPower;
                    bulletWithPath.ShootBullet();
                    // isMovingToEnemy = false;
                }
                shootTimer = 0f;
            }
            yield return null; // 等待下一帧
        }
        if(ant == null||!ant.activeSelf||distance >= collectEnemyRange)
        {
            isMovePaused = false;
            isMovingToEnemy = false;
            yield break;
        }
    }
}
