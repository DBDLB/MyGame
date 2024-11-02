using System;
using UnityEngine;
using System.Collections.Generic;

public class AntColony : MonoBehaviour
{
    public static AntColony instance;
    public static AntColony Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AntColony>();
                if (instance == null)
                {
                    GameObject singleton = new GameObject(typeof(AntColony).Name);
                    instance = singleton.AddComponent<AntColony>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    [Serializable]
    public class antPrefabs
    {
        public GameObject antPrefab;
        public int AntNum;
        public AntTrack AntTrack;
        public VariousAnt variousAnt;
    }
    
    public List<antPrefabs> prefabsAnt = new List<antPrefabs>(); // 蚂蚁预制体
    // public List<List<Vector3>> AntTrack.AntPathList = new List<List<Vector3>>(); // 蚂蚁路径列表

    public class VariousAnt
    {
        public GameObject antPrefab;
        public List<GameObject> ants = new List<GameObject>(); // 当前生成的蚂蚁列表
        public Queue<GameObject> antPool = new Queue<GameObject>(); // 对象池
        public AntTrack antTrack;
        public int totalAnts; // 总蚂蚁数量
        public int PreviousFrameAntPathListCount = 0;

        public VariousAnt(int totalAnts, AntTrack antTrack, GameObject antPrefab)
        {
            this.totalAnts = totalAnts;
            this.antTrack = antTrack;
            this.antPrefab = antPrefab;
        }
    }

    public static List<VariousAnt> variousAnts = new List<VariousAnt>(); // 当前生成的蚂蚁列表

    
    private int AntCount = 0;
    

    public bool test = false;
    void Start()
    {
        foreach (var antPrefab in prefabsAnt)
        {
            variousAnts.Add(new VariousAnt(antPrefab.AntNum, antPrefab.AntTrack, antPrefab.antPrefab));
            antPrefab.variousAnt = variousAnts[variousAnts.Count - 1];
        }
    }

    void Update()
    {
        foreach (var antPrefab in prefabsAnt)
        {
            if (antPrefab.AntNum != antPrefab.variousAnt.totalAnts)
            {
                antPrefab.variousAnt.totalAnts = antPrefab.AntNum;
            }
        }
        // 可以根据实际需求动态更新蚂蚁数量或路径
        bool needsReallocation;
        VariousAnt variousAnt = NeedsReallocation(out needsReallocation);
        if (needsReallocation)
        {
            ReallocateAnts(variousAnt);
        }
    }

    public void RecycleAnt(GameObject ant)
    {
        ant.SetActive(false); // 隐藏蚂蚁
        ant.GetComponent<Ant>().variousAnt.antPool.Enqueue(ant); // 回收蚂蚁
        ant.GetComponent<Ant>().waypoint.ants.Remove(ant.GetComponent<Ant>()); // 从路径上移除蚂蚁
        ant.GetComponent<Ant>().waypoint = null; // 清空蚂蚁的路径
        ReallocateAnts(ant.GetComponent<Ant>().variousAnt); // 回收后重新分配蚂蚁
    }

    void ReallocateAnts(VariousAnt variousAnt)
    {
        if (variousAnt.ants.Count<variousAnt.totalAnts)
        {
            int count = variousAnt.totalAnts - variousAnt.ants.Count;
            for (int i = 0; i < count; i++)
            {
                CreateAnt(variousAnt);
            }
        }

        // 计算每条路径应分配的蚂蚁数量
        int totalPaths = variousAnt.antTrack.AntPathList.Count;
        int antsPerPath = totalPaths > 0 ? (int)Math.Floor((double)variousAnt.totalAnts / totalPaths) : 0;
        // if (antsPerPath < 1)
        // {
        //     remainder = remainder - onTheRoad;
        // }
        // if (totalAnts > totalPaths)
        // {
        //     
        // }
        // else
        // {
        //     remainder = 0;
        // }
        int onTheRoad = CountAntsOnPath(variousAnt);
        
        if(totalPaths>0)
        {
            // 遍历路径，分配蚂蚁
            for (int i = 0; i < totalPaths; i++)
            {
                // 如果当前路径上的蚂蚁数量已满，则跳过
                if (variousAnt.antTrack.AntPathList[i].ants.Count >= antsPerPath)
                {
                    continue;
                }

                // 分配蚂蚁
                for (int j = 0; j < antsPerPath; j++)
                {
                    if (variousAnt.antPool.Count != 0&&onTheRoad<variousAnt.totalAnts)
                    {
                        GameObject ant = variousAnt.antPool.Dequeue();
                        ant.GetComponent<Ant>().waypoint = variousAnt.antTrack.AntPathList[i];
                        variousAnt.antTrack.AntPathList[i].ants.Add(ant.GetComponent<Ant>());
                        ant.SetActive(true);
                        onTheRoad++;
                    }
                }
            }

            
            int remainder = variousAnt.totalAnts - onTheRoad;
            // 分配余下的蚂蚁
            if (remainder > 0 && onTheRoad<variousAnt.totalAnts)
            {
                System.Random random = new System.Random();
                for (int i = 0; i < remainder; i++)
                {
                    int randomIndex = random.Next(totalPaths);
                    if (variousAnt.antPool.Count != 0)
                    {
                        GameObject ant = variousAnt.antPool.Dequeue();
                        ant.GetComponent<Ant>().waypoint = variousAnt.antTrack.AntPathList[randomIndex];
                        variousAnt.antTrack.AntPathList[randomIndex].ants.Add(ant.GetComponent<Ant>());
                        ant.SetActive(true);
                    }
                }
            }
        }
        variousAnt.PreviousFrameAntPathListCount = variousAnt.antTrack.AntPathList.Count;
        AntCount = variousAnt.totalAnts;
    }

    void CreateAnt(VariousAnt variousAnt)
    {
        GameObject ant = Instantiate(variousAnt.antPrefab, this.transform.position, Quaternion.identity);
        Ant antComponent = ant.GetComponent<Ant>();
        antComponent.colony = this; // 设置蚂蚁的引用
        ant.SetActive(false);
        variousAnt.ants.Add(ant);
        variousAnt.antPool.Enqueue(ant);
        antComponent.variousAnt = variousAnt;
    }
    
    //获取AntTrack.AntPathList中所有ant数量
    int CountAntsOnPath(VariousAnt variousAnt)
    {
        // 计算当前在指定路径上的蚂蚁数量
        int count = 0;
        foreach (var list in variousAnt.antTrack.AntPathList)
        {
            count += list.ants.Count;
        }
        return count;
    }
    
    // int CountAntsOnPath(List<Vector3> path)
    // {
    //     // 计算当前在指定路径上的蚂蚁数量
    //     int count = 0;
    //     foreach (var ant in allAnts)
    //     {
    //         Ant antComponent = ant.GetComponent<Ant>();
    //         if (antComponent.waypoint.pathList == path)
    //         {
    //             count++;
    //         }
    //     }
    //     return count;
    // }

    VariousAnt NeedsReallocation(out bool needsReallocation)
    {
        foreach (var variousAnt in variousAnts)
        {
            if (AntCount != variousAnt.totalAnts || variousAnt.PreviousFrameAntPathListCount != variousAnt.antTrack.AntPathList.Count)
            {
                needsReallocation = true;
                return variousAnt;
            }
        }
        needsReallocation = false;
        return null;
    }

}
