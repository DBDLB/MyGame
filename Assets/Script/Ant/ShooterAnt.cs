using UnityEngine;
using System.Collections;
using System.Linq;

public class ShooterAnt : Ant
{
    //收集食物范围
    public float collectFoodRange = 1;
    
    private int backCurrentWaypointIndex = 1;
    private bool isMovingToFood = false;
    public GameObject pickedFood;

    // protected override void Start()
    // {
    //     base.Start(); // 调用基类的 Start 方法
    //     // 这里可以添加 WorkerAnt 特有的初始化逻辑
    // }
    
    private void Update()
    {
        PerformAction();
    }

    protected Coroutine moveToFoodCoroutine;
    // protected override void PerformAction()
    // {
    //     if (!isMovingToFood)
    //     {
    //         Food food = GetNearestFood();
    //         if (food != null)
    //         {
    //             if (Vector3.Distance(transform.position, food.transform.position) < collectFoodRange)
    //             {
    //                 //检查food.hasAnt是否有自己有的话就不添加
    //                 foreach (var ant in food.hasAnt)
    //                 {
    //                     if (ant == this)
    //                     {
    //                         return;
    //                     }
    //                 }
    //                 bool moveTo = false;
    //                 //检查food.hasAnt是否已满
    //                 foreach (var ant in food.hasAnt)
    //                 {
    //                     if (ant == null)
    //                     {
    //                         food.AddAnt(this);
    //                         isMovingToFood = true;
    //                         isPatrolPaused = true;
    //                         moveToFoodCoroutine = StartCoroutine(MoveToFood(food));
    //                         break;
    //                     }
    //                 }
    //             }
    //         }
    //     }
    // }
    
    //获取离得最近的食物
    private Food GetNearestFood()
    {
        Food nearestFood = null;
        float minDistance = float.MaxValue;
        foreach (var food in FoodManager.Instance.foodList)
        {
            if (food != null)
            {
                float distance = Vector3.Distance(transform.position, food.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestFood = food;
                }
            }
        }

        return nearestFood;
    }
    
    // private IEnumerator MoveToFood(Food food)
    // {
    //     backCurrentWaypointIndex =currentWaypointIndex;
    //     // float distance = Vector3.Distance(food.transform.position, transform.position);
    //     // Vector3 direction = (food.transform.position - transform.position).normalized;
    //     float foodRadius = food.GetComponent<Collider>().bounds.extents.magnitude; // 获取敌人的半径
    //     while (food != null&&Vector3.Distance(transform.position, food.transform.position) > foodRadius)
    //     {
    //         transform.rotation = Quaternion.LookRotation((food.transform.position - transform.position).normalized);
    //         transform.position = Vector3.MoveTowards(transform.position, food.transform.position,
    //             Time.deltaTime * patrolSpeed*1.1f);
    //         yield return null; // 等待下一帧
    //     }
    //
    //     if (food != null)
    //     {
    //         pickedFood = food.OnFoodPicked(this);
    //     }
    //
    //     if (pickedFood != null)
    //     {
    //         // waypoint.ants.Remove(this);
    //         backToNest = true;
    //         StartCoroutine(HasFoodBackAntColony(pickedFood.GetComponent<Food>().foodValue));
    //     }
    //     else
    //     {
    //         isPatrolPaused = false;
    //         isMovingToFood = false;
    //     }
    // }
    //
    // //按照路径返回巢穴
    // private IEnumerator HasFoodBackAntColony(int foodValue)
    // {
    //     // StopCoroutine(moveToFoodCoroutine);
    //     // 循环巡逻
    //     while (waypoint != null)
    //     {
    //         // 如果到达终点，开始返回
    //         if (waypoint != null)
    //         {
    //             // 计算前进方向并朝向该方向
    //             // Vector3 directionBack = (waypoint.pathList[waypoint.pathList.Count - 2] - transform.position).normalized;
    //             Vector3 directionBack = (waypoint.pathList[^2] - transform.position).normalized;
    //             transform.rotation = Quaternion.LookRotation(directionBack);
    //             backToNest = true;
    //             // 返回路径
    //             for (int i = backCurrentWaypointIndex; i > 0; i--)
    //             {
    //                 while (waypoint != null&& i < waypoint.pathList.Count && Vector3.Distance(transform.position, waypoint.pathList[i]) > 0.1f)
    //                 {
    //                     float speed = patrolSpeed;
    //
    //                     // 计算前进方向并朝向该方向
    //                     Vector3 direction = (waypoint.pathList[i] - transform.position).normalized;
    //                     Quaternion targetRotation = Quaternion.LookRotation(direction);
    //                     transform.rotation =
    //                         Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed * 2);
    //
    //                     transform.position = Vector3.MoveTowards(transform.position, waypoint.pathList[i],
    //                         Time.deltaTime * speed);
    //                     yield return null; // 等待下一帧
    //                 }
    //             }
    //
    //             if (waypoint != null)
    //             {
    //                 // 回收蚂蚁
    //                 colony.foodCount += foodValue;
    //                 UIManager.Instance.ShowFoodCount();
    //                 //删除pickedFood
    //                 if (pickedFood!=null)
    //                 {
    //                     pickedFood.GetComponent<Food>().Die();
    //                 }
    //                 colony.RecycleAnt(gameObject);
    //                 isPatrolPaused = false;
    //                 isMovingToFood = false; // 到达食物位置后重置标志
    //                 yield break; // 结束协程
    //             }
    //         }
    //     }
    //     
    //     while (waypoint == null)
    //     {
    //         // 计算前进方向并朝向该方向
    //         Vector3 direction = (colony.transform.position - transform.position).normalized;
    //         Quaternion targetRotation = Quaternion.LookRotation(direction);
    //         transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * patrolSpeed*2);
    //         transform.position = Vector3.MoveTowards(transform.position, colony.transform.position, Time.deltaTime * patrolSpeed);
    //         // 检查是否到达巢穴位置
    //         if (Vector3.Distance(transform.position, colony.transform.position) < 0.1f)
    //         {
    //             // 回收蚂蚁
    //             colony.foodCount += foodValue;
    //             UIManager.Instance.ShowFoodCount();
    //             //删除pickedFood
    //             if (pickedFood!=null)
    //             {
    //                 pickedFood.GetComponent<Food>().Die();
    //             }
    //             colony.RecycleAnt(gameObject);
    //             isPatrolPaused = false;
    //             isMovingToFood = false; // 到达食物位置后重置标志
    //             yield break; // 结束协程
    //         }
    //         yield return null;
    //     }
    // }
    
    //重写Die方法
    protected override void PerformAction()
    {
        throw new System.NotImplementedException();
    }

    public override void Die()
    {
        // this.StopAllCoroutines();
        if (pickedFood != null)
        {
            InsectCarcass insectCarcass = pickedFood.GetComponent<InsectCarcass>();
            if(insectCarcass != null)
            {
                pickedFood.transform.localRotation = Quaternion.Euler(0, 0,  0);
                pickedFood.transform.SetParent(null, true);
                insectCarcass.ReleaseAnt();
            }
        }

        // ReleaseAnt();
        base.Die();
    }
    
    //释放子蚂蚁
    // public void ReleaseAnt()
    // {
    //     WorkerAnt[] children = GetComponentsInChildren<WorkerAnt>();
    //     if (children.Length > 1)
    //     {
    //         for (int i = 1; i < children.Length; i++)
    //         {
    //             children[i].transform.localRotation = Quaternion.Euler(0, 0, 0);
    //             children[i].transform.SetParent(null, true);
    //             children[i].transform.position = colony.transform.position;
    //             children[i].enabled = true;
    //             AntColony.Instance.RecycleAnt(children[i].gameObject);
    //         }
    //     }
    // }
}