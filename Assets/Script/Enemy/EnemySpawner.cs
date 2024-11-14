using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // 敌人预制体
    public Transform[] spawnPoints; // 生成点数组
    public int maxEnemies = 10; // 最大敌人数量

    private void OnEnable()
    {
        StartCoroutine(SpawnEnemyCoroutine());
    }

    private IEnumerator SpawnEnemyCoroutine()
    {
        while (true)
        {
            // 检测场景中敌人数量
            int enemyCount = GetEnemyCount();
            if (enemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
            yield return new WaitForSeconds(2f); // 每隔两秒生成一个敌人
        }
    }

    // 生成敌人方法
    private void SpawnEnemy()
    {
        // 随机选择一个生成点
        int spawnIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[spawnIndex];
        
        //随机朝向
        float randomAngle = Random.Range(0, 360);

        // 实例化敌人对象
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation * Quaternion.Euler(0, randomAngle, 0));
    }
    
    // 获取该预制体的敌人数量
    private int GetEnemyCount()
    {
        int num = 0;
        Enemy.EnemyType enemyType = enemyPrefab.GetComponent<Enemy>().enemyType;
        foreach (var enemy in FindObjectsOfType<Enemy>())
        {
            if (enemy.enemyType == enemyType)
            {
                num++;
            }
        }
        return num;
    }
}