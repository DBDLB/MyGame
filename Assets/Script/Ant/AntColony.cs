using UnityEngine;
using System.Collections.Generic;

public class AntColony : MonoBehaviour
{
    public GameObject antPrefab; // 蚂蚁预制体
    // public List<List<Vector3>> MouseTrack.AntPathList = new List<List<Vector3>>(); // 蚂蚁路径列表
    public int totalAnts = 10; // 总蚂蚁数量

    public static List<GameObject> allAnts = new List<GameObject>(); // 当前生成的蚂蚁列表
    private Queue<GameObject> antPool = new Queue<GameObject>(); // 对象池
    

    public bool test = false;
    void Start()
    {
        ReallocateAnts(); // 初始分配蚂蚁
    }

    void Update()
    {
        // 可以根据实际需求动态更新蚂蚁数量或路径
        if (test)
        {
            ReallocateAnts();
            test = false;
        }
    }

    public void RecycleAnt(GameObject ant)
    {
        ant.SetActive(false); // 隐藏蚂蚁
        antPool.Enqueue(ant); // 回收蚂蚁
        ant.GetComponent<Ant>().waypoints = null; // 清空蚂蚁的路径
        ReallocateAnts(); // 回收后重新分配蚂蚁
    }

    void ReallocateAnts()
    {
        if (allAnts.Count<totalAnts)
        {
            int count = totalAnts - allAnts.Count;
            for (int i = 0; i < count; i++)
            {
                CreateAnt(allAnts);
            }
        }

        // 计算每条路径应分配的蚂蚁数量
        int totalPaths = MouseTrack.AntPathList.Count;
        int antsPerPath = totalPaths > 0 ? totalAnts / totalPaths : 0;
        int remainder = totalPaths > 0 ? totalAnts % totalPaths : 0;

        if(totalPaths>0)
        {
            for (int i = 0; i < totalPaths; i++)
            {
                if (CountAntsOnPath(MouseTrack.AntPathList[i]) >= antsPerPath)
                {
                    continue;
                }

                for (int j = 0; j < antsPerPath; j++)
                {
                    if (antPool.Count != 0)
                    {
                        GameObject ant = antPool.Dequeue();
                        ant.GetComponent<Ant>().waypoints = MouseTrack.AntPathList[i];
                        ant.SetActive(true);
                    }
                }
            }

            if (remainder > 0)
            {
                System.Random random = new System.Random();
                for (int i = 0; i < remainder; i++)
                {
                    int randomIndex = random.Next(totalPaths);
                    if (antPool.Count != 0)
                    {
                        GameObject ant = antPool.Dequeue();
                        ant.GetComponent<Ant>().waypoints = MouseTrack.AntPathList[randomIndex];
                        ant.SetActive(true);
                    }
                }
            }
        }
    }

    void CreateAnt(List<GameObject> ants)
    {
        GameObject ant = Instantiate(antPrefab, this.transform.position, Quaternion.identity);
        Ant antComponent = ant.GetComponent<Ant>();
        antComponent.colony = this; // 设置蚂蚁的引用
        ant.SetActive(false);
        ants.Add(ant);
        antPool.Enqueue(ant);
    }
    
    int CountAntsOnPath(List<Vector3> path)
    {
        // 计算当前在指定路径上的蚂蚁数量
        int count = 0;
        foreach (var ant in allAnts)
        {
            Ant antComponent = ant.GetComponent<Ant>();
            if (antComponent.waypoints == path)
            {
                count++;
            }
        }
        return count;
    }

    bool NeedsReallocation()
    {
        // 根据实际需求定义重分配的条件
        return true; // 示例，具体条件根据实际情况实现
    }
    
}
