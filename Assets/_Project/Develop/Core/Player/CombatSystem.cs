using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;
using _Project.Develop.Core.Player;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using Random = UnityEngine.Random;

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

    [SerializeField] private bool hasShield = false; //ДОБАВИТЬ ПРОВЕРКУ В ИНВЕНТАРЕ ЕСТЬ ЛИ ЩИТ
    private bool isBlocking = false;
    private bool isBlockStarting = false;
    private float blockStartTime;

    [SerializeField] private Weapon equippedWeapon; // Текущее оружие
    [SerializeField] private Weapon secondaryWeapon; // Текущее оружие
    private bool isRangedCharging = false; // Флаг зарядки дальнобойного оружия
    private float rangedChargeTime = 0f;   // Текущее время зарядки
    private float rangedChargeDuration;    // Полное время зарядки из стата оружия

    private Vector3 moveInput;

    public RectTransform[] Crosshair;
    public RectTransform Cross;
    public Vector2[] CrosshairStartPos;
    public TweenerCore<Vector3,Vector3,VectorOptions>[] Tweens;

    [Space] [Header("Effects")] 
    public ParticleSystem ChargeSpellEffect;
    
    
    public MeshFilter WeaponMesh;
    public MeshFilter SecondaryWeaponMesh;

    [Header("Audio")] 
    [SerializeField] private AudioClip[] HitSounds;
    [SerializeField] private AudioClip[] SwingSounds;
    [SerializeField] private AudioClip SpellCast,SpellPrepare,BlockHit;

    [SerializeField] private SoundManager _manager = new();

    private bool mayAttack = true;
    void Start()
    {
        playerModel = new Creature("player1");
        //SetWeapon(new RangeWeapon("Sword"));
        currentMoveSpeed = baseMoveSpeed;

        // Инициализация дальнобойного оружия, если оно есть
        if (equippedWeapon != null && equippedWeapon is RangeWeapon)
        {
            UpdateRangedChargeDuration();
        }

        CrosshairStartPos = new Vector2[Crosshair.Length];
        Tweens = new TweenerCore<Vector3,Vector3,VectorOptions>[Crosshair.Length];
        for (int i = 0; i < Crosshair.Length; i++)
        {
            CrosshairStartPos[i] = Crosshair[i].position;
        }

        
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.MeeleWeapon, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Shield, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.RangeWeapon, Rarity.Rare);

        Inventory.OnInventoryStateChange += value => mayAttack = !value;
    }

    public void SetWeapon(Weapon weapon)
    {
        equippedWeapon = weapon;
        WeaponMesh.mesh = weapon.Mesh;
        print(weapon.Effects.Count);
        WeaponMesh.gameObject.SetActive(true);
        
        animator.SetTrigger("StopCharge");
        animator.SetBool("IsCharging", false);
        animator.SetBool("IsCharged", false);
        animator.ResetTrigger("RangedShot");
        isRangedCharging = false;
        rangedChargeTime = 0f;
        CloseCrosshair();
    }
    public void SetSecondaryWeapon(SecondaryWeapon weapon)
    {
        secondaryWeapon = weapon;
        SecondaryWeaponMesh.mesh = weapon.Mesh;
        print(weapon.Effects.Count);
        SecondaryWeaponMesh.gameObject.SetActive(true);

        if (weapon is Shield)
            hasShield = true;
    }

    public void RemoveWeapon()
    {
        equippedWeapon = null;
        WeaponMesh.gameObject.SetActive(false);
            
    }
    public void RemoveSecondaryWeapon()
    {
        secondaryWeapon = null;
        SecondaryWeaponMesh.gameObject.SetActive(false);
        hasShield = false;
    }

    private void OpenCrosshair(float time)
    {
        ChargeSpellEffect.Play();
        for (int i = 0; i < Crosshair.Length; i++)
        {
            Vector2 direction = Vector2.zero;

            switch (i)
            {
                case 0: 
                    direction = Vector2.up;
                    break;
                case 1: 
                    direction = Vector2.right;
                    break;
                case 2: 
                    direction = Vector2.down;
                    break;
                case 3: 
                    direction = Vector2.left;
                    break;
            }

            Tweens[i] = Crosshair[i].DOMove(CrosshairStartPos[i] + direction * 5f, time);
        }

        Tweens[0].onComplete += () => Cross.DOScale(Vector3.one, .25f);
    }

    private void CloseCrosshair()
    {
        ChargeSpellEffect.Stop();
        Cross.DOScale(Vector3.zero, .25f);
        for (int i = 0; i < Crosshair.Length; i++)
        {
            Tweens[i].Kill();
            Tweens[i] = Crosshair[i].DOMove(CrosshairStartPos[i], 0.25f);
        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
            ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Shield, Rarity.Legendary);
        playerModel.Update(Time.deltaTime);

        if (currentAttackIndex > 0 && Time.time - lastAttackTime > comboWindow)
        {
            ResetAttackCombo();
        }

        if (!mayAttack)
            return;

        // Ближний бой (ЛКМ)
        if (Input.GetMouseButtonDown(0) && !isBlocking && Time.time - lastAttackTime >= attackCooldown)
        {
            if (equippedWeapon != null && equippedWeapon is MeeleWeapon)
            {
                PerformAttack(); // Ближний бой
            }
        }

        // Дальнобойное оружие (ПКМ для зарядки) МАГИЯ
        if (equippedWeapon != null && equippedWeapon is RangeWeapon)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0) && !isBlocking && !isRangedCharging && Time.time - lastAttackTime >= attackCooldown)
            {
                StartRangedCharge();
                OpenCrosshair(equippedWeapon[StatType.AttackSpeed].CurrentValue);
            }
            else if (Input.GetKey(KeyCode.Mouse0) && isRangedCharging)
            {
                UpdateRangedCharge();
            }
            else if (Input.GetKeyUp(KeyCode.Mouse0) && isRangedCharging)
            {
                PerformRangedAttack();
                CloseCrosshair();
            }
        }
        if (hasShield) // Блок для ближнего боя
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
                    float damage = (playerModel[StatType.Strength].CurrentValue / 10 + 1) * equippedWeapon[StatType.Damage].CurrentValue;
                    enemy.TakeDamage(damage);
                    ((MeeleWeapon)equippedWeapon).ApplyEffects(enemy.skeletonModel);
                    Debug.Log($"Player attacked {hit.name} for {damage} damage!");
                }
            }
        }
        ((MeeleWeapon)equippedWeapon).FireProjectile();
        
        _manager.ProduceSound(transform.position,SwingSounds[Random.Range(0,SwingSounds.Length)]);
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
        _manager.ProduceSound(transform.position,SpellPrepare,true);
    }

    private void UpdateRangedCharge()
    {
        rangedChargeTime += Time.deltaTime * (1/equippedWeapon.Stats[StatType.AttackSpeed].CurrentValue);
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
            _manager.ProduceSound(transform.position,SpellCast);
            
        }
        else // Частичный заряд (пустой выстрел или ослабленный)
        {
            animator.SetTrigger("StopCharge");
            Debug.Log("Shot cancelled or partial charge!");
        }
        _manager.StopPlaying(SpellPrepare);
    }

    private void FireProjectile()
    {
        ((RangeWeapon)equippedWeapon).FireProjectile();
    }

    private void UpdateRangedChargeDuration()
    {
        if (equippedWeapon != null && equippedWeapon is RangeWeapon weapon)
        {
            float attackSpeed = equippedWeapon.Stats[StatType.AttackSpeed].CurrentValue;
            rangedChargeDuration = 1; // Чем выше AttackSpeed, тем быстрее зарядка
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
            //EndBlock();
            Debug.Log("Player blocked an attack!");
            _manager.ProduceSound(transform.position,BlockHit);
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
        _manager.ProduceSound(transform.position,HitSounds[Random.Range(0,HitSounds.Length)]);
        
    }
}

