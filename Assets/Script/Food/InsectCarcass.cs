using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InsectCarcass : Food
{
    public bool isPicked = false;
    public override GameObject OnFoodPicked(Ant ant)
    {
        if (!isPicked)
        {
            Transform antTransform = ant.transform;
            transform.localRotation = antTransform.localRotation;
            // cake.transform.position = antTransform.position;
            transform.SetParent(antTransform,true);
            transform.localPosition = new Vector3(0, 1, 0);
            isPicked = true;
            return gameObject;
        }
        return null;
    }
}
