using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifetime = 5f; // 子弹存活时间
    public float damage = 10f;  // 子弹的伤害值

    void Start()
    {
        // 一段时间后销毁子弹
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer != LayerMask.NameToLayer("Wall")||collision.gameObject.layer != LayerMask.NameToLayer("Enemy"))
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                // 获取敌人对象
                // Enemy enemy = collision.gameObject.GetComponent<Enemy>();
                // if (enemy != null)
                // {
                //     // 让敌人接收伤害
                //     enemy.TakeDamage(damage);
                // }
            }
            // 子弹碰撞到其他物体后销毁
            Destroy(gameObject);
        }

    }
    // private void OnTriggerEnter(Collider other)
    // {
    //     // 确保碰撞体是敌人
    //     if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
    //     {
    //         // 获取敌人对象
    //         Enemy enemy = other.GetComponent<Enemy>();
    //         if (enemy != null)
    //         {
    //             // 让敌人接收伤害
    //             enemy.TakeDamage(damage);
    //
    //             // 销毁子弹
    //             Destroy(gameObject);
    //         }
    //     }
    // }
}