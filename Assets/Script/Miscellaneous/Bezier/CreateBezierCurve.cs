using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CreateBezierCurve : MonoBehaviour
{
    // public int resolution = 10;//曲线的分辨率
    
    /// <param name="t">0到1的值，0获取曲线的起点，1获得曲线的终点</param>
    /// <param name="start">曲线的起始位置</param>
    /// <param name="center">决定曲线形状的控制点</param>
    /// <param name="end">曲线的终点</param>
    public Vector3 GetBezierPoint(float t, Vector3 start, Vector3 center, Vector3 end)
    {
        return (1 - t) * (1 - t) * start + 2 * t * (1 - t) * center + t * t * end;
    }
    
    private Vector3 InitControlPos(Vector3 startPos, Vector3 targetPos, float startAngle)
    {
        // 计算起始点和结束点之间的中点
        Vector3 midPoint = (startPos + targetPos) / 2f;
    
        //求单位向量    
        var vecSM  = midPoint - startPos;
        Vector3 zNormalized = vecSM.normalized;
        Vector3 zAxis = zNormalized;
        // 使用z轴单位向量和x轴单位向量的叉乘来得到y轴单位向量
        Vector3 xAxis = Vector3.Cross(Vector3.up,zAxis).normalized;
        Vector3 yAxis = Vector3.Cross( zAxis,xAxis).normalized;

        // 构建旋转矩阵
        Quaternion rotation = Quaternion.LookRotation(zAxis, yAxis);
        
        // 进行60度绕x轴旋转
        rotation *= Quaternion.Euler(-startAngle, 0f, 0f);
     
        // 计算旋转后的坐标
        Vector3 localRotationNormalizedPos = rotation * Vector3.forward;
        var cosValue = Mathf.Cos(Mathf.Deg2Rad * startAngle);
        Vector3 localRotationPos = localRotationNormalizedPos * (float)(vecSM.magnitude/cosValue);

        // 将局部坐标转换为世界坐标
        return (startPos + localRotationPos);
    }

    public Vector3[] GetBezierPath(Vector3 startPoint, Vector3 endPoint,float startAngle, int resolution = 10)
    {
        Vector3 bezierControlPoint = InitControlPos(startPoint, endPoint, startAngle);
        Vector3[] _path = new Vector3[resolution]; //resolution为int类型，表示要取得路径点数量，值越大，取得的路径点越多，曲线最后越平滑
        for (int i = 0; i < resolution; i++)
        {
            var t = (i + 1) / (float)resolution; //归化到0~1范围
            _path[i] = GetBezierPoint(t, startPoint, bezierControlPoint, endPoint); //使用贝塞尔曲线的公式取得t时的路径点
        }
        return _path;
    }
    
    
    //用Gizmos绘制贝塞尔曲线
    public Vector3 startPoint = new Vector3(0, 0, 0);
    public Vector3 endPoint = new Vector3(0, 0, 10);
    public float startAngle = 60;
    private void OnDrawGizmos()
    {
        Vector3 bezierControlPoint = InitControlPos(transform.position, endPoint, startAngle);
        Vector3[] path = GetBezierPath(transform.position, endPoint, startAngle);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, bezierControlPoint);
        Gizmos.DrawLine(endPoint, bezierControlPoint);
        Gizmos.DrawLine(transform.position, path[0]);
        for (int i = 0; i < path.Length - 1; i++)
        {
            Gizmos.DrawLine(path[i], path[i + 1]);
        }
    }

}
