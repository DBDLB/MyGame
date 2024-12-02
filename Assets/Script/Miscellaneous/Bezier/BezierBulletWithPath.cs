using UnityEngine;

[ExecuteAlways]
public class BezierBulletWithPath : MonoBehaviour
{
    public Vector3[] path;            // 预计算的贝塞尔曲线路径
    public float moveDuration = 2.0f; // 子弹移动时间

    private int currentIndex = 0;     // 当前路径点索引
    private float elapsedTime = 0.0f; // 累计时间

    public Vector3 endPoint = new Vector3(0, 0, 10);
    void OnEnable()
    {
        path = GetComponent<CreateBezierCurve>().GetBezierPath(transform.position,endPoint,
            Random.Range(30, 65), 10);
        if (path == null || path.Length == 0)
        {
            Debug.LogError("Path is not defined!");
            return;
        }
        currentIndex = 0; // 初始化索引
        elapsedTime = 0.0f; // 初始化时间

        // 初始化位置为路径的起点
        // transform.position = path[0];
    }

    void Update()
    {
        if (path == null || path.Length <= 1)
            return;

        // 更新时间
        elapsedTime += Time.deltaTime;

        // 总路径点数
        int totalPoints = path.Length;

        // 计算在路径上的位置
        float t = elapsedTime / moveDuration;
        if (t > 1.0f)
        {
            t = 1.0f; // 防止超出范围
            OnReachEnd();
        }

        // 当前段的索引计算
        float segmentLength = 1.0f / (totalPoints - 1);
        int nextIndex = Mathf.Min(currentIndex + 1, totalPoints - 1);

        // 插值因子计算
        float segmentT = (t - segmentLength * currentIndex) / segmentLength;

        // 移动到当前路径点与下一个路径点之间的位置
        transform.position = Vector3.Lerp(path[currentIndex], path[nextIndex], segmentT);

        // 如果已经到达当前段的终点，则更新索引
        if (segmentT >= 1.0f && currentIndex < totalPoints - 2)
        {
            currentIndex++;
        }
    }

    /// <summary>
    /// 当子弹到达目标位置时的回调
    /// </summary>
    private void OnReachEnd()
    {
        Debug.Log("Bullet reached the end!");
        // Destroy(gameObject); // 删除子弹
    }
}