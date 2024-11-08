using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cake : Food
{
    //蛋糕总量
    public int foodAllValue = 30; 
    public GameObject littleCake;
    public override GameObject OnFoodPicked(Ant ant)
    {
        // 生成小蛋糕
        GameObject cake = Instantiate(littleCake);
        // cake.transform.SetParent(ant.transform, false);
        // cake.transform.localPosition = new Vector3(0, 1, 0);
        Transform antTransform = ant.transform;
        cake.transform.localRotation = antTransform.localRotation;
        // cake.transform.position = antTransform.position;
        cake.transform.SetParent(antTransform,true);
        cake.transform.localPosition = new Vector3(0, 1, 0);
        cake.GetComponent<LittleCake>().foodValue = foodValue;
        foodAllValue -= foodValue;
        if (foodAllValue <= 0)
        {
            Destroy(gameObject);
        }
        
        return cake;
    }
}
