using UnityEngine;
using System.Collections;

public class WorkerAnt : Ant
{
    //收集食物范围
    public float collectFoodRange = 1;
    
    private int backCurrentWaypointIndex = 1;
    
    // protected override void Start()
    // {
    //     base.Start(); // 调用基类的 Start 方法
    //     // 这里可以添加 WorkerAnt 特有的初始化逻辑
    // }
    
    private void Update()
    {
        PerformAction();
    }

    protected override void PerformAction()
    {
        foreach (var food in FoodManager.Instance.foodList)
        {
            if (Vector3.Distance(transform.position, food.transform.position) < collectFoodRange)
            {
                StartCoroutine(MoveToFood(food.transform.position));
                break;
            }   
        }
    }
    
    private IEnumerator MoveToFood(Vector3 foodPosition)
    {
        float distance = Vector3.Distance(foodPosition, transform.position);
        while (Vector3.Distance(transform.position, foodPosition) > 0.1f)
        {
            Vector3 direction = (foodPosition - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * patrolSpeed * 2);
            transform.position = Vector3.MoveTowards(transform.position, foodPosition, Time.deltaTime * patrolSpeed/(distance%patrolSpeed));
            yield return null; // 等待下一帧
        }
    }
    
    
    //按照路径返回巢穴
    private IEnumerator HasFoodBackAntColony(int foodValue)
    {
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
                for (int i = backCurrentWaypointIndex; i >= 0; i--)
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
                    colony.RecycleAnt(gameObject);
                    yield break; // 结束协程
                }
            }
        }
    }
}