using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager
{
    public static EnemyManager instance;
    public static EnemyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new EnemyManager();
            }
            return instance;
        }
    }
    
    
    public List<Enemy> enemyList = new List<Enemy>(); // 敌人列表
}
