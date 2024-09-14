using UnityEngine;

public class SingleShotShooter : BulletShooter
{
    public float fireRate = 1f;  // 发射间隔（秒）
    private float nextTimeToFire = 0f; // 下一次可以发射的时间
    void Update()
    {
        AimAtMouse();
        // 检查是否按下鼠标左键，并且当前时间超过了下次发射的时间
        if (Input.GetMouseButtonDown(0) && Time.time >= nextTimeToFire)
        {
            // 更新下一次发射的时间（当前时间 + 发射间隔）
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
    }

    public override void Shoot()
    {
        // 发射子弹的方向是枪口的正前方
        Vector3 shootDirection = firePoint.forward;
        FireBullet(shootDirection);
    }
}