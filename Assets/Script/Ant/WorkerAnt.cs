using UnityEngine;
using System.Collections;

public class WorkerAnt : Ant
{
    //收集食物范围
    public float collectFoodRange = 1;
    
    private int backCurrentWaypointIndex = 1;
    private bool isMovingToFood = false;
    private GameObject pickedFood;
    
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
    protected override void PerformAction()
    {
        if (!isMovingToFood)
        {
            foreach (var food in FoodManager.Instance.foodList)
            {
                if (food != null)
                {
                    if (Vector3.Distance(transform.position, food.transform.position) < collectFoodRange)
                    {
                        if (food.gameObject.GetComponent<Cake>() == null)
                        {
                            FoodManager.Instance.foodList.Remove(food);
                        }

                        isMovingToFood = true;
                        isPatrolPaused = true;
                        moveToFoodCoroutine = StartCoroutine(MoveToFood(food));
                        break;
                    }
                }
            }
        }
    }
    
    private IEnumerator MoveToFood(Food food)
    {
        backCurrentWaypointIndex =currentWaypointIndex;
        // float distance = Vector3.Distance(food.transform.position, transform.position);
        Vector3 direction = (food.transform.position - transform.position).normalized;
        while (food != null&&Vector3.Distance(transform.position, food.transform.position) > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
            transform.position = Vector3.MoveTowards(transform.position, food.transform.position,
                Time.deltaTime * patrolSpeed);
            yield return null; // 等待下一帧
        }

        if (food != null)
        {
            pickedFood = food.OnFoodPicked(this);
        }

        if (pickedFood != null)
        {
            StartCoroutine(HasFoodBackAntColony(pickedFood.GetComponent<Food>().foodValue));
        }
        else
        {
            isPatrolPaused = false;
            isMovingToFood = false;
        }
    }
    
    
    //按照路径返回巢穴
    private IEnumerator HasFoodBackAntColony(int foodValue)
    {
        // StopCoroutine(moveToFoodCoroutine);
        // 循环巡逻
        while (waypoint != null)
        {
            // 如果到达终点，开始返回
            if (waypoint != null)
            {
                // 计算前进方向并朝向该方向
                // Vector3 directionBack = (waypoint.pathList[waypoint.pathList.Count - 2] - transform.position).normalized;
                Vector3 directionBack = (waypoint.pathList[^2] - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(directionBack);
                backToNest = true;
                // 返回路径
                for (int i = backCurrentWaypointIndex; i > 0; i--)
                {
                    while (waypoint != null && Vector3.Distance(transform.position, waypoint.pathList[i]) > 0.1f)
                    {
                        float speed = patrolSpeed;

                        // 计算前进方向并朝向该方向
                        Vector3 direction = (waypoint.pathList[i] - transform.position).normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation =
                            Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed * 2);

                        transform.position = Vector3.MoveTowards(transform.position, waypoint.pathList[i],
                            Time.deltaTime * speed);
                        yield return null; // 等待下一帧
                    }
                }

                if (waypoint != null)
                {
                    // 回收蚂蚁
                    colony.foodCount += foodValue;
                    AntColony.instance.ShowFoodCount();
                    //删除pickedFood
                    Destroy(pickedFood);
                    colony.RecycleAnt(gameObject);
                    isPatrolPaused = false;
                    isMovingToFood = false; // 到达食物位置后重置标志
                    yield break; // 结束协程
                }
            }
        }
        
        while (waypoint == null)
        {
            // 计算前进方向并朝向该方向
            Vector3 direction = (colony.transform.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * patrolSpeed*2);
            transform.position = Vector3.MoveTowards(transform.position, colony.transform.position, Time.deltaTime * patrolSpeed);
            // 检查是否到达巢穴位置
            if (Vector3.Distance(transform.position, colony.transform.position) < 0.1f)
            {
                // 回收蚂蚁
                colony.foodCount += foodValue;
                AntColony.instance.ShowFoodCount();
                colony.DeletePathRecycleAnt(gameObject);
                isPatrolPaused = false;
                //删除pickedFood
                Destroy(pickedFood);
                isMovingToFood = false; // 到达食物位置后重置标志
                yield break; // 结束协程
            }
            yield return null;
        }
    }
}