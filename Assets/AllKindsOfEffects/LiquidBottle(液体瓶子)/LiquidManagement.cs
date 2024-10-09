using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiquidManagement : MonoBehaviour
{
    private static LiquidManagement instance;

    private LiquidManagement()
    {
        
    }

    public static LiquidManagement Instance
    {
        get
        {
            if (instance ==null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    if (liquidInstance==null)
                    {
                        liquidInstance = new GameObject("Liquid Instance");
                        DontDestroyOnLoad(liquidInstance);
                        instance = liquidInstance.AddComponent<LiquidManagement>();
                    }
                }
#else
                 if (liquidInstance==null)
                        {
                            liquidInstance = new GameObject("Liquid Instance");
                            DontDestroyOnLoad(liquidInstance);
                            instance = liquidInstance.AddComponent<LiquidManagement>();
                        }
#endif

            }

            return instance;
        }
    }

    private static GameObject liquidInstance;
    private List<GameObject> liquidObj = new List<GameObject>();
    private List<LiquidScript> liquids = new List<LiquidScript>();
    List<LiquidScript> liquidsToRemove = new List<LiquidScript>();
    List<LiquidScript> liquidsToFixedRemove = new List<LiquidScript>();
    private int frameTimes = 2;
    public event Action<GameObject> LiquidObjectDestroyed;

    // Start is called before the first frame update
    /*void Start()
    {
        foreach (var VARIABLE in liquidObj)
        {
            liquids.Add(VARIABLE.GetComponent<LiquidScript>());
        }
    }*/

    private int time = 0;
    // Update is called once per frame
    void Update()
    {
        if (time%frameTimes == 0)
        {
            liquidsToRemove.Clear();
            foreach (var VARIABLE in liquids)
            {
                if (GameObjIsHas(VARIABLE))
                {
                    VARIABLE.LiquidUpdate();
                }
                else
                {
                    liquidsToRemove.Add(VARIABLE);
                  
                }


            }

            foreach (var VARIABLE in liquidsToRemove)
            {
                DestoryLiquid(liquidObj[liquids.IndexOf(VARIABLE)],VARIABLE);
            }
        }

        time++;
    }

    private bool GameObjIsHas(LiquidScript lst)
    {
        if (lst&&lst.material&&lst.meshRenderer)
        {
            if (liquidObj[liquids.IndexOf(lst)])
            {
                return true;
            }
        }

        return false;
    }

    private int timeFixed = 0;
    // Update is called once per frame
    /*void FixedUpdate()
    {
        if (timeFixed%frameTimes == 0)
        {
            liquidsToFixedRemove.Clear();
            foreach (var VARIABLE in liquids)
            {
                if (VARIABLE)
                {
                    VARIABLE.LiquidFixedUpdate();
                }
                else
                {
                    liquidsToFixedRemove.Add(VARIABLE);
                  
                }
            
            }

            foreach (var VARIABLE in liquidsToFixedRemove)
            {
                DestoryLiquid(liquidObj[liquids.IndexOf(VARIABLE)],VARIABLE);
            }
        }

        timeFixed++;
    }*/
    //应用退出时消除
    private void OnApplicationQuit()
    {
        Destroy(liquidInstance);
        liquidInstance = null;
        instance = null; // 设置实例为 null
    }
    public void AddLiquid(GameObject obj)
    {
        if (!liquidObj.Contains(obj))
        {
            liquidObj.Add(obj);
            LiquidScript liquidChild = obj.GetComponent<LiquidScript>();
            if (!liquids.Contains(liquidChild))
            {
                liquids.Add(liquidChild);
            }
        }
        
    }
    public void DestoryLiquid(GameObject obj,LiquidScript ls)
    {
        if (liquidObj.Contains(obj)&&liquids.Contains(ls))
        {
            liquids.Remove(ls);
            liquidObj.Remove(obj);
        }
    }
/// <summary>
/// 设置每隔多少帧刷新
/// </summary>
    public void SetUpdateTimeFrame(int frameTime)
    {
        frameTimes = frameTime;
    }
}
