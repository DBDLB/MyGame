using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    public class EnemyTypeSpawner
    {
        public GameObject enemyPrefab; // 敌人预制体
        public Transform[] spawnPoints; // 生成点数组
        public int maxEnemies = 10; // 最大敌人数量
    }
    
    public List<EnemyTypeSpawner> enemyTypesSpawner = new List<EnemyTypeSpawner>(); // 敌人类型列表
    private void OnEnable()
    {
        StartCoroutine(SpawnEnemyCoroutine());
    }

    private IEnumerator SpawnEnemyCoroutine()
    {
        while (true)
        {
            foreach (var enemyType in enemyTypesSpawner)
            {
                // 检测场景中敌人数量
                int enemyCount = GetEnemyCount(enemyType);
                if (enemyCount < enemyType.maxEnemies)
                {
                    SpawnEnemy(enemyType);
                }
            }
            yield return new WaitForSeconds(2f); // 每隔两秒生成一个敌人
        }
    }

    // 生成敌人方法
    private void SpawnEnemy(EnemyTypeSpawner enemyTypeSpawner)
    {
        // 随机选择一个生成点
        int spawnIndex = Random.Range(0, enemyTypeSpawner.spawnPoints.Length);
        Transform spawnPoint = enemyTypeSpawner.spawnPoints[spawnIndex];
        
        //随机朝向
        float randomAngle = Random.Range(0, 360);

        // 实例化敌人对象
        GameObject enemy = Instantiate(enemyTypeSpawner.enemyPrefab, spawnPoint.position, spawnPoint.rotation * Quaternion.Euler(0, randomAngle, 0));
    }

    // 获取该预制体的敌人数量
    private int GetEnemyCount(EnemyTypeSpawner enemyTypeSpawner)
    {
        int num = 0;
        Enemy.EnemyType enemyType = enemyTypeSpawner.enemyPrefab.GetComponent<Enemy>().enemyType;
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