using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class InputMain : MonoBehaviour
{   
    [Serializable]
    public class PlayerData
    {
        public Vector4 currentPosition;
        public Vector4 lastPosition;
        public Vector3 uvOffset;
        public bool isPlayer;
    }

    public PlayerData pdata = new PlayerData();
    private void OnEnable()
    {
        if (pdata.isPlayer)
        {
            //往playerDatas的最前面插入当前InputMain
            if (DrawTrailingFeature.playerDatas.Contains(this))
            {
                DrawTrailingFeature.playerDatas.Remove(this);
            }
            DrawTrailingFeature.playerDatas.Insert(0, this);
        }
        else
        {
            if (!DrawTrailingFeature.playerDatas.Contains(this))
            {
                DrawTrailingFeature.playerDatas.Add(this);
            }
        }

    }

    private void OnDisable()
    {
        if (DrawTrailingFeature.playerDatas.Contains(this))
        {
            DrawTrailingFeature.playerDatas.Remove(this);
        }
    }

    // private void Update()
    // {
    //     pdata.currentPosition = transform.position;
    //     pdata.uvOffset = pdata.currentPosition - pdata.lastPosition;
    //     pdata.lastPosition =  pdata.currentPosition;
    // }
}
