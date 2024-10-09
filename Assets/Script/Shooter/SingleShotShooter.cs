using UnityEngine;

public class SingleShotShooter : BulletShooter
{
    public float fireRate = 1f;  // 发射间隔（秒）
    private float nextTimeToFire = 0f; // 下一次可以发射的时间


    public override void Shoot()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextTimeToFire)
        {
            // 更新下一次发射的时间（当前时间 + 发射间隔）
            nextTimeToFire = Time.time + fireRate;
            // 发射子弹的方向是枪口的正前方
            Vector3 shootDirection = firePoint.forward;
            FireBullet(shootDirection);
        }
    }
}