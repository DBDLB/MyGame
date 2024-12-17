using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowGizmos : MonoBehaviour
{
    public Vector3 fishFlocksPosition;
    public Vector3 fishFlocksScale;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(fishFlocksPosition, fishFlocksScale);
    }
}
