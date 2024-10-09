using System.Collections;
using UnityEngine;

public class BurstShooter : BulletShooter
{
    public int burstCount = 3;       // 连发数量
    public float burstInterval = 0.2f; // 连发间隔
    public float shootingInterval = 0.5f; // 每轮连发之间的间隔

    private bool isShooting = false;



    public override void Shoot()
    {
        //我们用协程来处理连发
        if (Input.GetMouseButton(0) && !isShooting)
        {
            StartCoroutine(ShootContinuously());
        }
    }

    private IEnumerator ShootContinuously()
    {
        isShooting = true;

        while (Input.GetMouseButton(0)) // 当玩家持续按住按钮时
        {
            for (int i = 0; i < burstCount; i++)
            {
                // 计算发射方向
                Vector3 shootDirection = firePoint.forward;
                FireBullet(shootDirection); // 发射一颗子弹
                yield return new WaitForSeconds(burstInterval); // 等待一段时间再发射下一发
            }

            // 连发结束后，等待一定时间才能进行下一轮发射
            yield return new WaitForSeconds(shootingInterval);
        }

        isShooting = false;
    }
}