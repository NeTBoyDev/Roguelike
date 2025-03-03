using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    private Creature playerModel;
    [SerializeField] private Animator animator;

    [SerializeField] private float baseMoveSpeed = 5f;
    private float currentMoveSpeed;
    [SerializeField] private float attackRange = 1.5f; // Для ближнего боя
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float comboWindow = 0.5f;
    private float lastAttackTime;
    private int currentAttackIndex = 0;

    [SerializeField] private bool hasShield = true;
    private bool isBlocking = false;
    private bool isBlockStarting = false;
    private float blockStartTime;

    [SerializeField] private Weapon equippedWeapon; // Текущее оружие
    private bool isRangedCharging = false; // Флаг зарядки дальнобойного оружия
    private float rangedChargeTime = 0f;   // Текущее время зарядки
    private float rangedChargeDuration;    // Полное время зарядки из стата оружия

    private Vector3 moveInput;

    void Start()
    {
        playerModel = new Creature("player1");
        equippedWeapon = new MeeleWeapon("Sword");
        currentMoveSpeed = baseMoveSpeed;

        // Инициализация дальнобойного оружия, если оно есть
        if (equippedWeapon != null && equippedWeapon is RangeWeapon)
        {
            UpdateRangedChargeDuration();
        }
    }

    void Update()
    {
        playerModel.Update(Time.deltaTime);

        if (currentAttackIndex > 0 && Time.time - lastAttackTime > comboWindow)
        {
            ResetAttackCombo();
        }

        // Ближний бой (ЛКМ)
        if (Input.GetMouseButtonDown(0) && !isBlocking && Time.time - lastAttackTime >= attackCooldown)
        {
            if (equippedWeapon == null || equippedWeapon is not RangeWeapon)
            {
                PerformAttack(); // Ближний бой
            }
        }

        // Дальнобойное оружие (ПКМ для зарядки)
        if (equippedWeapon != null && equippedWeapon is RangeWeapon)
        {
            if (Input.GetKeyDown(KeyCode.Mouse1) && !isBlocking && !isRangedCharging && Time.time - lastAttackTime >= attackCooldown)
            {
                StartRangedCharge();
            }
            else if (Input.GetKey(KeyCode.Mouse1) && isRangedCharging)
            {
                UpdateRangedCharge();
            }
            else if (Input.GetKeyUp(KeyCode.Mouse1) && isRangedCharging)
            {
                PerformRangedAttack();
            }
        }
        else if (hasShield) // Блок для ближнего боя
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

        if (isBlockStarting && Time.time - blockStartTime >= animator.GetCurrentAnimatorStateInfo(0).length / 2)
        {
            isBlockStarting = false;
            animator.SetBool("IsBlocking", true);
        }

        animator.SetBool("IsBlocking", isBlocking && !isBlockStarting);
    }

    private void PerformAttack() // Ближний бой
    {
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
        Vector3 attackPoint = transform.position + Vector3.up + attackDirection * attackRange * 0.5f;

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint, attackRange * 0.5f);
        foreach (var hit in hitEnemies)
        {
            if (hit.CompareTag("Enemy"))
            {
                SkeletonAI enemy = hit.GetComponent<SkeletonAI>();
                if (enemy != null)
                {
                    float damage = playerModel[StatType.Strength].CurrentValue + equippedWeapon[StatType.Damage].CurrentValue;
                    enemy.TakeDamage(damage);
                    Debug.Log($"Player attacked {hit.name} for {damage} damage!");
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 attackDirection = transform.forward;
        Vector3 attackPoint = transform.position + Vector3.up + attackDirection * attackRange * 0.5f;

        Gizmos.DrawSphere(attackPoint, attackRange * 0.5f);
    }

    private void StartRangedCharge()
    {
        isRangedCharging = true;
        rangedChargeTime = 0f;
        animator.SetBool("IsCharging", true);
        UpdateRangedChargeDuration(); // Обновляем время зарядки на случай смены оружия
    }

    private void UpdateRangedCharge()
    {
        rangedChargeTime += Time.deltaTime;
        if (rangedChargeTime >= rangedChargeDuration)
        {
            // Зарядка завершена, но ждём отпускания кнопки для выстрела
            animator.SetBool("IsCharging", false);
            animator.SetBool("IsCharged", true);
        }
    }

    private void PerformRangedAttack()
    {
        isRangedCharging = false;
        animator.SetBool("IsCharged", false);
        animator.SetBool("IsCharging", false);
        lastAttackTime = Time.time;

        if (rangedChargeTime >= rangedChargeDuration) // Полный заряд
        {
            animator.SetTrigger("RangedShot"); // Анимация выстрела
            FireProjectile();
        }
        else // Частичный заряд (пустой выстрел или ослабленный)
        {
            animator.SetTrigger("RangedShot");
            Debug.Log("Shot cancelled or partial charge!");
        }
    }

    private void FireProjectile()
    {
        // Здесь должна быть логика создания и запуска снаряда (можно настроить позже)
        Debug.Log("Ranged attack fired!");
        // Пример: создание снаряда и применение урона в определённой зоне
    }

    private void UpdateRangedChargeDuration()
    {
        if (equippedWeapon != null && equippedWeapon is RangeWeapon weapon)
        {
            float baseChargeTime = weapon[StatType.AttackSpeed].CurrentValue; // Базовое время зарядки из оружия
            float attackSpeed = equippedWeapon.Stats[StatType.AttackSpeed].CurrentValue;
            rangedChargeDuration = baseChargeTime / attackSpeed; // Чем выше AttackSpeed, тем быстрее зарядка
        }
    }

    private void ResetAttackCombo()
    {
        currentAttackIndex = 0;
        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        animator.ResetTrigger("Attack3");
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
            EndBlock();
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

    public void TakeDamage(float value)
    {
        if (isBlocking)
        {
            OnHitWhileBlocking();
            playerModel.Stats[StatType.Health].Modify(-value/3);
        }
        else
        {
            playerModel.Stats[StatType.Health].Modify(-value);
        }
        
        
    }
}

