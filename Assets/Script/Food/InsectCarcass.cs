using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InsectCarcass : Food
{
    public bool isPicked = false;
    private Ant firstAnt;
    private Bounds foodBounds;
    private int randomX = 1;
    
    private float antSpeed;
    public override GameObject OnFoodPicked(WorkerAnt ant)
    {
        randomX*=-1;
        if (!isPicked)
        {
            firstAnt = ant;
            foodBounds = this.GetComponent<Collider>().bounds; // 获取食物的半径
            // ant.minDistanceToPreviousAnt = 0f;
            antSpeed = ant.patrolSpeed/accommodateAntCount;
            ant.patrolSpeed = antSpeed;
            Transform antTransform = ant.transform;
            transform.localRotation = antTransform.localRotation;
            transform.SetParent(antTransform,true);
            transform.localPosition = new Vector3(0, 0, 0);
            isPicked = true;
            return gameObject;
        }
        else
        {
            if (ant.pickedFood == null)
            {

                //停止ant所有协程
                firstAnt.patrolSpeed += antSpeed;
                ant.StopAllCoroutines();
                ant.enabled = false;
                Transform antTransform = firstAnt.transform;
                ant.transform.localRotation = antTransform.localRotation;
                //计算一个随机的1，-1
                ant.transform.position = transform.position; //+ new Vector3(randomX*(foodBounds.size.x), 0, randomZ);
                ant.transform.SetParent(antTransform, true);
                float randomZ =
                    (UnityEngine.Random.Range(0, foodBounds.size.z) * 0.5f - antTransform.localScale.z * 0.5f) *
                    (UnityEngine.Random.Range(0, 2) * 2 - 1);
                ant.transform.localPosition +=
                    new Vector3(randomX * (float)(foodBounds.size.x* 0.5 + antTransform.localScale.y * 0.5f-0.1f), 0, randomZ);
                //ant.transform.localRotation = Quaternion.LookRotation(transform.position - ant.transform.position);
                //ant.transform.localPosition += new Vector3(0, 0, (float)(randomZ));
                return null;
            }
            else
            {
                return null;
            }
        }
    }

    public override void Die()
    {
        ReleaseAnt();
        base.Die();
    }
    
    public void ReleaseAnt()
    {
        for (int i = 1; i < hasAnt.Length; i++)
        {
            if (hasAnt[i] != null)
            {
                //旋转归零
                hasAnt[i].transform.localRotation = Quaternion.Euler(0, 0, 0);
                hasAnt[i].transform.SetParent(null, true);
                hasAnt[i].transform.position = firstAnt.transform.position;
                hasAnt[i].enabled = true;
                AntColony.Instance.RecycleAnt(hasAnt[i].gameObject);
            }

        }
        isPicked = false;
        hasAnt = new Ant[accommodateAntCount];
    }
}
