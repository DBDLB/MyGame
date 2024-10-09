using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterManagement : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            GetComponentInChildren<ShallowWater>().enabled = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            GetComponentInChildren<ShallowWater>().enabled = false;
        }
    }
}
