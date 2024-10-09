using System;
using UnityEngine;

public abstract class BulletShooter : MonoBehaviour
{
    public GameObject bulletPrefab;     // 子弹预制体
    public float bulletSpeed = 20f;     // 子弹速度
    public Transform firePoint;         // 子弹发射的位置
    public bool playerControlled = false;

    // 抽象方法，派生类必须实现该方法
    public abstract void Shoot();
    
    void Update()
    {
        AimAtMouse();
        Shoot();
    }

    // 这个方法创建并发射子弹，派生类可以选择调用它
    protected void FireBullet(Vector3 direction)
    {
        // 创建子弹实例
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // 获取子弹的Rigidbody组件，用于设置速度
        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // 使子弹按指定的方向和速度移动
            rb.velocity = direction * bulletSpeed;
        }
    }
    
    protected void AimAtMouse()
    {
        if(playerControlled)
        {
            // 创建一条从摄像机到鼠标指针的射线
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 如果射线击中了场景中的对象
            if (Physics.Raycast(ray, out hit))
            {
                // 计算发射器朝向鼠标所在位置的方向
                Vector3 targetPoint = hit.point;

                // 调整目标点的y值为发射器的y值，这样发射器只在水平面上旋转
                targetPoint.y = transform.position.y;

                // 让发射器看向目标点
                transform.LookAt(targetPoint);
            }
        }
    }
}