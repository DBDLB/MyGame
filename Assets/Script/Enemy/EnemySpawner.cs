using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;          // 敌人预制体
    public Transform[] spawnPoints;         // 敌人生成的点
    public float spawnInterval = 5f;        // 敌人生成间隔时间
    public int maxEnemies = 10;             // 场上最大敌人数
    private int currentEnemyCount = 0;      // 当前场上的敌人数

    private float timer = 0f;               // 生成计时器

    void Update()
    {
        // 每帧更新计时器
        timer += Time.deltaTime;

        // 检查是否达到生成时间并且场上敌人数未达到上限
        if (timer >= spawnInterval && currentEnemyCount < maxEnemies)
        {
            SpawnEnemy();  // 生成敌人
            timer = 0f;    // 重置计时器
        }
    }

    // 生成敌人的方法
    private void SpawnEnemy()
    {
        // 随机选择一个生成点
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomIndex];

        // 在生成点生成敌人
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

        // 增加当前敌人计数
        currentEnemyCount++;

        // 为敌人添加死亡事件，减少计数器
        newEnemy.GetComponent<Enemy>().OnDeath += EnemyDied;
    }

    // 当敌人死亡时调用
    private void EnemyDied()
    {
        currentEnemyCount--;
    }
}