using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlyingAnt : Ant
{
    public Vector3 attackPosition;
    public GameObject shooterAntBullet;
    public float bulletMoveSpeed = 2.0f; // 子弹移动时间
    public int attackPower = 5; // 每次造成的伤害量
    public float attackRange = 2.0f; // 攻击范围
    private void Update()
    {
        // PerformAction();
    }

    // protected override void OnEnable()
    // {
    //     // base.OnEnable();
    // }

    protected Coroutine moveToEnemyCoroutine;
    
    protected override IEnumerator Patrol()
    {
        while (waypoint!=null)
        {
            int pathListCount = waypoint.pathList.Count;
            if (pathListCount > 1)
            {
                transform.rotation = Quaternion.LookRotation((waypoint.pathList[1] - transform.position).normalized);
            }
            // 前往当前巡逻点
            if (waypoint != null && !backToNest)
            {
                for (int i = 1; i < pathListCount; i++)
                {
                    currentWaypointIndex++;
                    while (waypoint != null && i < waypoint.pathList.Count && Vector3.Distance(transform.position, waypoint.pathList[i]) > 0.1f)
                    {
                        float speed = patrolSpeed;
                        // 计算前进方向并朝向该方向
                        Vector3 direction = (waypoint.pathList[i] - transform.position).normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation =
                            Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed * 2);

                        transform.position = Vector3.MoveTowards(transform.position, waypoint.pathList[i],
                            Time.deltaTime * speed);
                        yield return null; // 等待下一帧
                    }

                    if (i == pathListCount/2)
                    {
                        GameObject bullet = Instantiate(shooterAntBullet, transform.position, Quaternion.identity);
                        FlyingAntBullet bulletWithPath = bullet.GetComponent<FlyingAntBullet>();
                        bulletWithPath.endPoint = attackPosition;
                        bulletWithPath.speed = bulletMoveSpeed;
                        bulletWithPath.attackPower = attackPower;
                        bulletWithPath.attackRange = attackRange;
                        bulletWithPath.ShootBullet();
                    }
                }
            }


            if (waypoint != null&&health > 0)
            {
                Destroy(waypoint.lineRenderer.gameObject);
                waypoint = null;
                gameObject.SetActive(false);
                variousAnt.antPool.Enqueue(this.gameObject);
                yield return null;
            }
        }
    }

    // 攻击行为
    protected override void PerformAction()
    {
        
    }

    public override void Die()
    {
        if (!isDead)
        {
            isDead = true;
            if (waypoint != null)
            {
                Destroy(waypoint.lineRenderer.gameObject);
            }
            waypoint = null;
            // 默认的死亡行为，可以在子类中重写
            variousAnt.ants.Remove(this.gameObject);
            if (waypoint != null)
            {
                waypoint.ants.Remove(this); // 从路径上移除蚂蚁
                waypoint = null; // 清空蚂蚁的路径
            }
            UIManager.Instance.ShowAntCount(variousAnt, variousAnt.antPrefab.antPrefab.GetComponent<Ant>().antType);
            // AntColony.instance.DeleteAnt(antType);
            Destroy(gameObject);            
        }
    }
}