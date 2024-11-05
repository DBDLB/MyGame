using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodManager : MonoBehaviour
{   
    public static FoodManager instance;
    public static FoodManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<FoodManager>();
                if (instance == null)
                {
                    GameObject singleton = new GameObject(typeof(FoodManager).Name);
                    instance = singleton.AddComponent<FoodManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    public List<Food> foodList = new List<Food>(); // 食物列表
}
