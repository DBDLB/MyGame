using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class AntColony : MonoBehaviour
{
    //枚举蚂蚁种类
    public enum AntType
    {
        WorkerAnt,
        SoldierAnt,
        ShooterAnt,
        FlyingAnt,
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
        public int AntStartNum;
        public AntTrack AntTrack;
        public VariousAnt variousAnt;
    }
    
    public List<antPrefabs> prefabsAnts = new List<antPrefabs>(); // 蚂蚁预制体
    public FlyingAntTrack flyingAntTrack;
    // public List<List<Vector3>> AntTrack.AntPathList = new List<List<Vector3>>(); // 蚂蚁路径列表

    public class VariousAnt
    {
        public antPrefabs antPrefab;
        public List<GameObject> ants = new List<GameObject>(); // 当前生成的蚂蚁列表
        public Queue<GameObject> antPool = new Queue<GameObject>(); // 对象池
        public AntTrack antTrack;
        // public int totalAnts; // 总蚂蚁数量
        public int PreviousFrameAntPathListCount = 0;

        public VariousAnt( AntTrack antTrack, antPrefabs antPrefab)
        {
            // this.totalAnts = totalAnts;
            this.antTrack = antTrack;
            this.antPrefab = antPrefab;
        }
    }

    public GameObject ChildAntColony;

    public static List<VariousAnt> variousAnts = new List<VariousAnt>(); // 当前生成的蚂蚁列表

    public int foodCount = 20;
    
    private int AntCount = 0;

    [HideInInspector]public GameObject AntColonyPosition;


    void Start()
    {
        //复制ChildAntColony到指定位置
        // Instantiate(ChildAntColony, new(-13.6f,0.5f,4.9f), Quaternion.identity).SetActive(true);
        
        foreach (var antPrefab in prefabsAnts)
        {
            variousAnts.Add(new VariousAnt(antPrefab.AntTrack, antPrefab));
            antPrefab.variousAnt = variousAnts[variousAnts.Count - 1];
            //根据antPrefab.AntNum生成蚂蚁
            for (int i = 0; i < antPrefab.AntStartNum; i++)
            {
                CreateAnt(antPrefab.variousAnt);
            }
        }
        UIManager.Instance.ShowFoodCount();
        foreach (var variousAnt in variousAnts)
        {
            if (variousAnt.antPrefab.antPrefab.GetComponent<Ant>().antType != AntType.FlyingAnt)
            {
                StartCoroutine(ReallocateAntsCoroutine(variousAnt));
            }
            else
            {
                FlyingAntTrack.flyingAnt = variousAnt;
            }
            UIManager.Instance.ShowAntCount(variousAnt, variousAnt.antPrefab.antPrefab.GetComponent<Ant>().antType);
        }
    }

    // void Update()
    // {
    //     CheckAndReallocateAnts();
    // }
    //
    #region 蚂蚁生成与回收
    // //检查和重新分配蚂蚁
    // private void CheckAndReallocateAnts()
    // {
    //     foreach (var antPrefab in prefabsAnts)
    //     {
    //         if (antPrefab.AntNum != antPrefab.variousAnt.totalAnts)
    //         {
    //             antPrefab.variousAnt.totalAnts = antPrefab.AntNum;
    //         }
    //     }
    //     // 可以根据实际需求动态更新蚂蚁数量或路径
    //     bool needsReallocation;
    //     VariousAnt variousAnt = NeedsReallocation(out needsReallocation);
    //     if (needsReallocation)
    //     {
    //         // StartCoroutine(ReallocateAntsCoroutine(variousAnt));
    //     }
    // }
    

    public void RecycleAnt(GameObject ant)
    {
        ant.SetActive(false); // 隐藏蚂蚁
        Ant antComponent = ant.GetComponent<Ant>();
        antComponent.variousAnt.antPool.Enqueue(ant); // 回收蚂蚁
        if (antComponent.waypoint != null)
        {
            antComponent.waypoint.ants.Remove(antComponent); // 从路径上移除蚂蚁
            antComponent.waypoint = null; // 清空蚂蚁的路径
        }

        if (ant.GetComponent<WorkerAnt>() != null)
        {
            ant.GetComponent<WorkerAnt>().ReleaseAnt();
        }
        // StartCoroutine(ReallocateAntsCoroutine(ant.GetComponent<Ant>().variousAnt));// 回收后重新分配蚂蚁
        // ant.GetComponent<Ant>().isPatrolPaused = false;
    }

    private IEnumerator ReallocateAntsCoroutine(VariousAnt variousAnt)
    {
        while (true)
        {
            
            //清除variousAnts中antTrack中ants为null的蚂蚁
            for (int i = 0; i < variousAnt.antTrack.AntPathList.Count; i++)
            {
                for (int j = 0; j < variousAnt.antTrack.AntPathList[i].ants.Count; j++)
                {
                    if (variousAnt.antTrack.AntPathList[i].ants[j] == null)
                    {
                        variousAnt.antTrack.AntPathList[i].ants.RemoveAt(j);
                    }
                }
            }

            // if (variousAnt.ants.Count < variousAnt.antPrefab.AntNum)
            // {
            //     int count = variousAnt.antPrefab.AntNum - variousAnt.ants.Count;
            //     for (int i = 0; i < count; i++)
            //     {
            //         CreateAnt(variousAnt);
            //     }
            // }

            while (variousAnt.antPool.Count != 0)
            {
                bool hasLeaveOver = true;
                //判断哪一条路径上的蚂蚁数量最少
                int pathAntsCount = variousAnt.ants.Count/Math.Max(variousAnt.antTrack.AntPathList.Count,1);
                foreach (var Path in variousAnt.antTrack.AntPathList)
                {
                    if (variousAnt.antPool.Count == 0 || Path.ants.Count >= pathAntsCount)
                    {
                        hasLeaveOver = true;
                        continue;
                    }
                    GameObject ant = variousAnt.antPool.Dequeue();
                    ant.GetComponent<Ant>().waypoint = Path;
                    Path.ants.Add(ant.GetComponent<Ant>());
                    ant.SetActive(true);
                    hasLeaveOver = false;
                }

                if (hasLeaveOver)
                {
                    AntTrack.AntPath pathWithLeastAnts = FindPathWithLeastAnts(variousAnt);
                    if (pathWithLeastAnts != null &&variousAnt.antPool.Count != 0)
                    {
                        GameObject ant = variousAnt.antPool.Dequeue();
                        ant.GetComponent<Ant>().waypoint = pathWithLeastAnts;
                        pathWithLeastAnts.ants.Add(ant.GetComponent<Ant>());
                        ant.SetActive(true);
                    }
                }
                yield return new WaitForSeconds(0.4f);
            }


            // // 计算每条路径应分配的蚂蚁数量
            // int totalPaths = variousAnt.antTrack.AntPathList.Count;
            // int antsPerPath = totalPaths > 0 ? (int)Math.Floor((double)variousAnt.totalAnts / totalPaths) : 0;
            // // if (antsPerPath < 1)
            // // {
            // //     remainder = remainder - onTheRoad;
            // // }
            // // if (totalAnts > totalPaths)
            // // {
            // //     
            // // }
            // // else
            // // {
            // //     remainder = 0;
            // // }
            // int onTheRoad = CountAntsOnPath(variousAnt);
            //
            // if(totalPaths>0)
            // {
            //     // 遍历路径，分配蚂蚁
            //     for (int i = 0; i < totalPaths; i++)
            //     {
            //         // 如果当前路径上的蚂蚁数量已满，则跳过
            //         if (variousAnt.antTrack.AntPathList[i].ants.Count >= antsPerPath)
            //         {
            //             continue;
            //         }
            //
            //         // 分配蚂蚁
            //         for (int j = 0; j < antsPerPath; j++)
            //         {
            //             if (variousAnt.antPool.Count != 0&&onTheRoad<variousAnt.totalAnts)
            //             {
            //                 GameObject ant = variousAnt.antPool.Dequeue();
            //                 ant.GetComponent<Ant>().waypoint = variousAnt.antTrack.AntPathList[i];
            //                 variousAnt.antTrack.AntPathList[i].ants.Add(ant.GetComponent<Ant>());
            //                 ant.SetActive(true);
            //                 onTheRoad++;
            //             }
            //         }
            //     }
            //
            //     
            //     int remainder = variousAnt.totalAnts - onTheRoad;
            //     List<AntTrack.AntPath> allotAntPath = new List<AntTrack.AntPath>();
            //     foreach (var antPath in variousAnt.antTrack.AntPathList)
            //     {
            //         if (antPath.ants.Count <= antsPerPath)
            //         {
            //             allotAntPath.Add(antPath);
            //         }
            //
            //     }
            //     // 分配余下的蚂蚁
            //     if (remainder > 0 && onTheRoad<variousAnt.totalAnts)
            //     {
            //         System.Random random = new System.Random();
            //         for (int i = 0; i < remainder; i++)
            //         {
            //             int randomIndex = random.Next(allotAntPath.Count);
            //             if (variousAnt.antPool.Count != 0)
            //             {
            //                 GameObject ant = variousAnt.antPool.Dequeue();
            //                 ant.GetComponent<Ant>().waypoint = allotAntPath[randomIndex];
            //                 allotAntPath[randomIndex].ants.Add(ant.GetComponent<Ant>());
            //                 ant.SetActive(true);
            //             }
            //         }
            //     }
            // }
            variousAnt.PreviousFrameAntPathListCount = variousAnt.antTrack.AntPathList.Count;
            yield return null;
        }
    }

    private AntTrack.AntPath FindPathWithLeastAnts(VariousAnt variousAnt)
    {
        List<AntTrack.AntPath> pathsWithLeastAnts = new List<AntTrack.AntPath>();
        int minAntCount = int.MaxValue;

        foreach (var path in variousAnt.antTrack.AntPathList)
        {
            int antCount = path.ants.Count;
            if (antCount < minAntCount)
            {
                minAntCount = antCount;
                pathsWithLeastAnts.Clear();
                pathsWithLeastAnts.Add(path);
            }
            else if (antCount == minAntCount)
            {
                pathsWithLeastAnts.Add(path);
            }
        }

        if (pathsWithLeastAnts.Count > 0)
        {
            System.Random random = new System.Random();
            int randomIndex = random.Next(pathsWithLeastAnts.Count);
            return pathsWithLeastAnts[randomIndex];
        }

        return null;
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
    // int CountAntsOnPath(VariousAnt variousAnt)
    // {
    //     // 计算当前在指定路径上的蚂蚁数量
    //     int count = 0;
    //     foreach (var list in variousAnt.antTrack.AntPathList)
    //     {
    //         count += list.ants.Count;
    //     }
    //     return count;
    // }
    
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

    // VariousAnt NeedsReallocation(out bool needsReallocation)
    // {
    //     foreach (var variousAnt in variousAnts)
    //     {
    //         if (variousAnt.antPrefab.AntNum != variousAnt.totalAnts || variousAnt.PreviousFrameAntPathListCount != variousAnt.antTrack.AntPathList.Count)
    //         {
    //             needsReallocation = true;
    //             return variousAnt;
    //         }
    //     }
    //     needsReallocation = false;
    //     return null;
    // }
    #endregion
    
    #region 食物的添加与减少
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
                        // prefabsAnt.AntNum++;
                        CreateAnt(prefabsAnt.variousAnt);
                        foodCount -= prefabsAnt.antPrefab.GetComponent<Ant>().price;
                        UIManager.Instance.ShowAntCount(prefabsAnt.variousAnt, (AntType)antType);
                    }
                }
            }
            UIManager.Instance.ShowFoodCount();
        }
    }
    
    // public void DeleteAnt(AntType antType)
    // {
    //     foreach (var prefabsAnt in prefabsAnts)
    //     {
    //         if (prefabsAnt.antPrefab.GetComponent<Ant>().antType == (AntType)antType)
    //         {
    //             prefabsAnt.AntNum--;
    //         }
    //     }
    // }
    #endregion
}
