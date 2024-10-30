using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawCurve : MonoBehaviour
{
    public Vector3[] points;
    public int amountRadio = 20;
    public List<Vector3> showPoints;
    // Start is called before the first frame update
    private void OnDrawGizmos()
    {
        PathHelper.DrawPathHelper(points, Color.red);
        PathHelper.GetWayPoints(points, amountRadio, ref showPoints);
        
        GetComponent<LineRenderer>().positionCount = showPoints.Count;
        GetComponent<LineRenderer>().SetPositions(showPoints.ToArray());
    }
}
