using System;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class AntColony : MonoBehaviour
{
    //枚举蚂蚁种类
    public enum AntType
    {
        WorkerAnt,
        SoldierAnt
    }
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
    
    public List<antPrefabs> prefabsAnts = new List<antPrefabs>(); // 蚂蚁预制体
    // public List<List<Vector3>> AntTrack.AntPathList = new List<List<Vector3>>(); // 蚂蚁路径列表

    public class VariousAnt
    {
        public antPrefabs antPrefab;
        public List<GameObject> ants = new List<GameObject>(); // 当前生成的蚂蚁列表
        public Queue<GameObject> antPool = new Queue<GameObject>(); // 对象池
        public AntTrack antTrack;
        public int totalAnts; // 总蚂蚁数量
        public int PreviousFrameAntPathListCount = 0;

        public VariousAnt(int totalAnts, AntTrack antTrack, antPrefabs antPrefab)
        {
            this.totalAnts = totalAnts;
            this.antTrack = antTrack;
            this.antPrefab = antPrefab;
        }
    }

    public static List<VariousAnt> variousAnts = new List<VariousAnt>(); // 当前生成的蚂蚁列表
    
    public TextMeshProUGUI textMeshPro;
    public int foodCount = 20;
    
    private int AntCount = 0;
    
    
    void Start()
    {
        foreach (var antPrefab in prefabsAnts)
        {
            variousAnts.Add(new VariousAnt(antPrefab.AntNum, antPrefab.AntTrack, antPrefab));
            antPrefab.variousAnt = variousAnts[variousAnts.Count - 1];
        }
        ShowFoodCount();
    }

    void Update()
    {
        CheckAndReallocateAnts();
    }

    #region 蚂蚁生成与回收
    //检查和重新分配蚂蚁
    private void CheckAndReallocateAnts()
    {
        foreach (var antPrefab in prefabsAnts)
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
    
    public void DeletePathRecycleAnt(GameObject ant)
    {
        ant.SetActive(false); // 隐藏蚂蚁
        ant.GetComponent<Ant>().variousAnt.antPool.Enqueue(ant); // 回收蚂蚁
        ReallocateAnts(ant.GetComponent<Ant>().variousAnt); // 回收后重新分配蚂蚁
    }

    void ReallocateAnts(VariousAnt variousAnt)
    {
        //清除variousAnts中antTrack中ants为null的蚂蚁
        for (int i = 0; i < variousAnt.antTrack.AntPathList.Count; i++)
        {
            for (int j = 0; j < variousAnt.antTrack.AntPathList[i].ants.Count; j++)
            {
                if (variousAnt.antTrack.AntPathList[i].ants[j] == null)
                {
                    variousAnt.antTrack.AntPathList[i].ants.RemoveAt(j);
                    variousAnt.antPrefab.AntNum--;
                }
            }
        }

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
            List<AntTrack.AntPath> allotAntPath = new List<AntTrack.AntPath>();
            foreach (var antPath in variousAnt.antTrack.AntPathList)
            {
                if (antPath.ants.Count <= antsPerPath)
                {
                    allotAntPath.Add(antPath);
                }

            }
            // 分配余下的蚂蚁
            if (remainder > 0 && onTheRoad<variousAnt.totalAnts)
            {
                System.Random random = new System.Random();
                for (int i = 0; i < remainder; i++)
                {
                    int randomIndex = random.Next(allotAntPath.Count);
                    if (variousAnt.antPool.Count != 0)
                    {
                        GameObject ant = variousAnt.antPool.Dequeue();
                        ant.GetComponent<Ant>().waypoint = allotAntPath[randomIndex];
                        allotAntPath[randomIndex].ants.Add(ant.GetComponent<Ant>());
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
        GameObject ant = Instantiate(variousAnt.antPrefab.antPrefab, this.transform.position, Quaternion.identity);
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
    #endregion
    
    #region 食物的添加与减少
    public void ShowFoodCount()
    {
        textMeshPro.text = "食物数量：" + foodCount;
    }
    
    //消耗食物生成蚂蚁
    public void ConsumeFoodAndCreateAnt(int antType)
    {
        if (foodCount > 0)
        {
            foreach (var prefabsAnt in prefabsAnts)
            {
                // if (prefabsAnt.antPrefab.GetComponent<Ant>().antType == antType)
                // {
                //     prefabsAnt.AntNum++;
                //     foodCount -= prefabsAnt.antPrefab.GetComponent<Ant>().price;
                // }
                if (prefabsAnt.antPrefab.GetComponent<Ant>().antType == (AntType)antType)
                {
                    if (foodCount >= prefabsAnt.antPrefab.GetComponent<Ant>().price)
                    {
                        prefabsAnt.AntNum++;
                        foodCount -= prefabsAnt.antPrefab.GetComponent<Ant>().price;
                    }
                }
            }
            ShowFoodCount();
        }
    }
    #endregion
    

}
