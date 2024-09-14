using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 单例实例
    public static GameManager Instance { get; private set; }

    // 玩家位置
    public Vector3 playerPosition;

    // 场上怪物数量
    public int monsterCount;

    // 游戏时间（秒）
    public float gameTime;

    // 在游戏开始时初始化单例
    void Awake()
    {
        // 如果没有实例存在，设置这个为唯一实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 保持在场景切换时不会被销毁
        }
        else
        {
            Destroy(gameObject); // 防止多个实例
        }
    }

    void Update()
    {
        // 更新游戏时间
        gameTime += Time.deltaTime;

        // （可选）这里可以更新一些共享数据，比如玩家位置
        if (PlayerController.Instance != null) // 假设玩家有一个PlayerController单例
        {
            playerPosition = PlayerController.Instance.transform.position;
        }
    }

    // 用于更新怪物数量
    public void UpdateMonsterCount(int count)
    {
        monsterCount = count;
    }
}