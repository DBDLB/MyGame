using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage); // 受到指定伤害值
    void Die();                  // 处理死亡逻辑
}

