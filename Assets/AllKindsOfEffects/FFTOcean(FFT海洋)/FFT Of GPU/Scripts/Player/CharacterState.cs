using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 人物状态
/// </summary>
public class CharacterState : MonoBehaviour
{
    static public AnimatorStateInfo anim;
    public List<GameObject> Key = new List<GameObject>();

    private void Awake()
    {
        Common.character = this.gameObject;
    }
}
