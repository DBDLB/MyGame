using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LittleCake : Food
{
    public override GameObject OnFoodPicked(WorkerAnt ant)
    {
        return null;
    }
    protected virtual void OnEnable()
    {
        FoodManager.Instance.foodList.Remove(this);
    }
}
