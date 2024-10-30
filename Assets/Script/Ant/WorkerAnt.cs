using UnityEngine;

public class WorkerAnt : Ant
{
    // protected override void Start()
    // {
    //     base.Start(); // 调用基类的 Start 方法
    //     // 这里可以添加 WorkerAnt 特有的初始化逻辑
    // }

    protected override void PerformAction()
    {
        // WorkerAnt 特定的行为，例如搬运资源
        Debug.Log("Worker Ant is performing an action!");
    }

    private void Update()
    {
        // 可以根据条件决定何时执行特定行为
        if (Input.GetKeyDown(KeyCode.Space)) // 示例：按下空格键时执行
        {
            PerformAction();
        }
    }
}