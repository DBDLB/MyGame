using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowBody : MonoBehaviour
{
    public float offset = 0.5f;
    //获取父级的带PlayerController脚本的物体
    private GameObject GetPlayerController()
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (parent.GetComponentInChildren<PlayerController>() != null)
            {
                return parent.GetComponentInChildren<PlayerController>().gameObject;
            }
        }
        return null;
    }

    private GameObject playerController;
    private void Start()
    {
        playerController = GetPlayerController();
    }

    private void Update()
    {   

        this.transform.position = new Vector3(playerController.transform.position.x, offset, playerController.transform.position.z);
    }
}
