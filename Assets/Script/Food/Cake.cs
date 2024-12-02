using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cake : Food
{
    //蛋糕总量
    public GameObject littleCake;
    public override GameObject OnFoodPicked(WorkerAnt ant)
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
        cake.GetComponent<LittleCake>().AddAnt(ant);
        foodValue -= cake.GetComponent<LittleCake>().foodValue;
        if (foodValue <= 0)
        {
            FoodManager.Instance.foodList.Remove(this);
            Destroy(gameObject);
        }
        
        return cake;
    }

    public override void AddAnt(Ant ant)
    {
        return;
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
    }
}
