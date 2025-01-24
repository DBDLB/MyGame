using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteAlways]
public class Test : MonoBehaviour
{
    public List<Vector3> path;            // 预计算的贝塞尔曲线路径
    public Transform targetEnemy;
    public int resolution = 10;
    public int amountRadio = 20;
    public float Angle = 45;
    public Vector3 test;
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        path.Add(test);
        // path = CreateBezierCurve.GetBezierPath(transform.position, targetEnemy.position, Random.Range(30, 65), 10);
        path.AddRange( CreateBezierCurve.GetBezierPath(transform.position, targetEnemy.position, Angle, resolution,false));
        path.AddRange(CreateBezierCurve.GetBezierPath(targetEnemy.position, transform.position, Angle, resolution,false));
        PathHelper.GetWayPoints(path.ToArray(), amountRadio, ref path);
        Debug.Log(path.Count);
        // PathHelper.DrawPathHelper(path.ToArray(), Color.red);
        for (int i = 0; i < path.Count - 1; i++)
        {
            Gizmos.DrawLine(path[i], path[i + 1]);
        }

        path.Clear();
    }
}
