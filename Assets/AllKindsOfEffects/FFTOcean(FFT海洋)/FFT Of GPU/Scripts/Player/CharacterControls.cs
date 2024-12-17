using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色控制
/// </summary>
public class CharacterControls : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;
    AnimatorStateInfo stateinfo;
    private float battleTime;
    private float attackATime;

    public float speed = 1;

    private bool isAttack = false;
    private bool attackA = true;

    // public GameObject idleWeapon;
    // public GameObject battleWeapon;

    // public ParticleSystem attack_effects_A;
    // public ParticleSystem attack_effects_A_1;
    // public ParticleSystem attack_effects_A_bloom;
    // public ParticleSystem attack_effects_B;
    // public ParticleSystem attack_effects_B_1;
    // public ParticleSystem attack_effects_B_bloom;
    // public float attack_effects_wait;

    // public GameObject attackCollider;

    // private Dash dash;//冲刺


    private void Start()
    {
        controller = this.GetComponent<CharacterController>();
        animator = transform.GetComponentInChildren<Animator>();
        // dash = GetComponent<Dash>();//获取冲刺脚本
    }

    private void Update()
    {
        PlayerMove();
        Attack();
        if (transform.position.y != 1)//检测角色是否在地面上
            transform.position = new Vector3(transform.position.x, 1, transform.position.z);
    }

    private void PlayerMove()//角色移动
    {
        stateinfo = animator.GetCurrentAnimatorStateInfo(0);// 获取当前状态机状态信息
        CharacterState.anim = stateinfo;//给角色状态赋值

        // if (stateinfo.IsTag("Attack") && stateinfo.normalizedTime >= 0.85f)
        // {
        //     attackCollider.SetActive(false);//关闭攻击触发器
        // }

        if ((stateinfo.IsTag("Attack") && stateinfo.normalizedTime <= 1.0f/* && stateinfo.normalizedTime >= 0.1f*/))//判断是否在播放攻击动画，如果是退出
        {
            isAttack = false;
            return;
        }
        else if (isAttack)
        {
            if (attackA)
            {
                animator.SetTrigger("AttackA");
                attackATime = 0;
                attackA = false;
                // StartCoroutine(PlayEffects_A());//阻塞攻击A
            }
            else
            {
                animator.SetTrigger("AttackB");
                attackA = true;
                // StartCoroutine(PlayEffects_B());//阻塞攻击B
            }
            isAttack = false;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;
        animator.SetFloat("running", direction.sqrMagnitude);//设置状态机参数running
        Vector3 move = direction * Time.deltaTime * speed;

        // dash.DashMove();

        controller.Move(move);
        if (direction != Vector3.zero)
        {
            this.transform.forward = direction;//设置角色朝向
        }
    }

    private void Attack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            animator.SetBool("BattleStand", true);
            // idleWeapon.SetActive(false);
            // battleWeapon.SetActive(true);
            isAttack = true;
            battleTime = 0;
        }

        battleTime += Time.deltaTime;
        if (battleTime >= 10)//控制进入战斗状态时间
        {
            animator.SetBool("BattleStand", false);
            // idleWeapon.SetActive(true);
            // battleWeapon.SetActive(false);
            battleTime = 0;
        }

        attackATime += Time.deltaTime;
        if (attackATime >= 2)
        {
            attackA = true;
            attackATime = 0;
        }
    }
    // 播放攻击A特效
    // private IEnumerator PlayEffects_A()
    // {
    //     // 等待特效等待时间
    //     yield return new WaitForSeconds(attack_effects_wait);
    //     // 播放攻击A特效
    //     attack_effects_A.Play();
    //     attack_effects_A_1.Play();
    //     attack_effects_A_bloom.Play();
    //
    //     attackCollider.SetActive(true);//开启攻击触发器
    // }

    // 播放攻击B特效
    // private IEnumerator PlayEffects_B()
    // {
    //     // 等待特效等待时间
    //     yield return new WaitForSeconds(attack_effects_wait);
    //     // 播放攻击B特效
    //     attack_effects_B.Play();
    //     attack_effects_B_1.Play();
    //     attack_effects_B_bloom.Play();
    //
    //     attackCollider.SetActive(true);//开启攻击触发器
    // }

}
