using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    private Creature playerModel;
    [SerializeField]private Animator animator;

    [SerializeField] private float baseMoveSpeed = 5f;
    private float currentMoveSpeed;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float comboWindow = 0.5f; 
    private float lastAttackTime;
    private int currentAttackIndex = 0; 

    [SerializeField] private bool hasShield = true; 
    private bool isBlocking = false;               
    private bool isBlockStarting = false;          
    private float blockStartTime;

    private Vector3 moveInput;

    void Start()
    {
        playerModel = new Creature("player1");

        currentMoveSpeed = baseMoveSpeed;
    }

    void Update()
    {
        playerModel.Update(Time.deltaTime);



        if (currentAttackIndex > 0 && Time.time - lastAttackTime > comboWindow)
        {
            ResetAttackCombo();
        }

        if (Input.GetMouseButtonDown(0) && !isBlocking && Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack();
        }

        if (hasShield)
        {
            if (Input.GetMouseButtonDown(1) && !isBlocking)
            {
                StartBlock();
            }
            else if (Input.GetMouseButtonUp(1) && isBlocking)
            {
                EndBlock();
            }
        }

        if (isBlockStarting && Time.time - blockStartTime >= animator.GetCurrentAnimatorStateInfo(0).length/2)
        {
            isBlockStarting = false;
            animator.SetBool("IsBlocking", true);
        }

        animator.SetBool("IsBlocking", isBlocking && !isBlockStarting);
    }

    private void PerformAttack()
    {
        print(animator.GetCurrentAnimatorStateInfo(1).IsName("1H_Melee_Attack_Slice_Horizontal"));

        if (currentAttackIndex > 2)
            return;
        
        currentAttackIndex = Mathf.Min(currentAttackIndex + 1, 3);
        lastAttackTime = Time.time;
        
        switch (currentAttackIndex)
        {
            case 1:
                animator.SetTrigger("Attack1");
                break;
            case 2:
                animator.SetTrigger("Attack2");
                break;
            case 3:
                animator.SetTrigger("Attack3");
                break;
        }

        Vector3 attackDirection = transform.forward;
        Vector3 attackPoint = transform.position + attackDirection * attackRange * 0.5f;

        // Проверка попадания
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint, attackRange * 0.5f);
        foreach (var hit in hitEnemies)
        {
            if (hit.CompareTag("Enemy"))
            {
                SkeletonAI enemy = hit.GetComponent<SkeletonAI>();
                if (enemy != null)
                {
                    float damage = playerModel.Stats[StatType.Damage].CurrentValue;
                    enemy.TakeDamage(damage);
                    Debug.Log($"Player attacked {hit.name} for {damage} damage!");
                }
            }
        }
    }

    private void ResetAttackCombo()
    {
        currentAttackIndex = 0;
        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
    }

    private void StartBlock()
    {
        isBlocking = true;
        isBlockStarting = true;
        blockStartTime = Time.time;
        animator.SetTrigger("BlockStart");
    }

    private void EndBlock()
    {
        isBlocking = false;
        isBlockStarting = false;
        animator.SetBool("IsBlocking", false);
        animator.ResetTrigger("BlockStart");
    }

    public void OnHitWhileBlocking()
    {
        if (isBlocking)
        {
            animator.SetTrigger("BlockHit");
            Debug.Log("Player blocked an attack!");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 attackPoint = transform.position + transform.forward * attackRange * 0.5f;
        Gizmos.DrawWireSphere(attackPoint, attackRange * 0.5f);
    }

    public void ApplyEffect(Effect effect)
    {
        playerModel.ApplyEffect(effect);
    }
}
