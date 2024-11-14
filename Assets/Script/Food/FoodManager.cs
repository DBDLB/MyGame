using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodManager
{   
    public static FoodManager instance;
    public static FoodManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new FoodManager();
            }
            return instance;
        }
    }

    public List<Food> foodList = new List<Food>(); // 食物列表
}
