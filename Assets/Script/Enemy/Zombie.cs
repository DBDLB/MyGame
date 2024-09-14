using UnityEngine;

public class Zombie : Enemy
{
    protected override void AttackPlayer()
    {
        // 实现僵尸攻击玩家的逻辑
        Debug.Log("Zombie attacks the player!");
        // 示例：减少玩家的生命值
        // Player.Instance.TakeDamage(attackPower);
    }

    protected override void Die()
    {
        base.Die(); // 调用基类的死亡逻辑
        // 额外的僵尸死亡处理逻辑
        Debug.Log("Zombie has died!");
    }
}