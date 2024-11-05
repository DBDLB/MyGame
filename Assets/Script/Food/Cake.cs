using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cake : Food
{
    public GameObject littleCake;
    public override void OnFoodPicked(Ant ant)
    {
        // 生成小蛋糕
        GameObject cake = Instantiate(littleCake, ant.transform);
        cake.transform.localPosition = new Vector3(0, 0.5f, 0);
        cake.GetComponent<LittleCake>().foodValue = foodValue;
    }
}
