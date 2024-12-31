using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class OceanInOut : MonoBehaviour
{
    public Underwater Underwater;
    public CinemachineVirtualCamera VirtualCamera;
    public bool IsUnderwater;
    public int Priority;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Underwater.enabled = IsUnderwater;
            VirtualCamera.Priority = Priority;
        }
    }
}
