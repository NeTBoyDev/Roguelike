using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using _Project.Develop.Core.Player;
using Cysharp.Threading.Tasks;
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

    [SerializeField] private bool hasShield = false; // ДОБАВИТЬ ПРОВЕРКУ В ИНВЕНТАРЕ ЕСТЬ ЛИ ЩИТ
    private bool isBlocking = false;
    private bool isBlockStarting = false;
    private float blockStartTime;

    [SerializeField] private Weapon equippedWeapon; // Текущее оружие
    [SerializeField] private Weapon secondaryWeapon; // Второе оружие
    private bool isRangedCharging = false; // Флаг зарядки дальнобойного оружия
    private float rangedChargeTime = 0f;   // Текущее время зарядки
    private float rangedChargeDuration;    // Полное время зарядки из стата оружия
    private bool isReloading = false;      // Флаг перезарядки
    private float reloadTimer = 0f;        // Таймер перезарядки

    private Vector3 moveInput;

    public RectTransform[] Crosshair;
    public RectTransform Cross;
    public Vector2[] CrosshairStartPos;
    public TweenerCore<Vector3, Vector3, VectorOptions>[] Tweens;

    [Space] [Header("Effects")] 
    public ParticleSystem ChargeSpellEffect;
    
    public MeshFilter WeaponMesh;
    public MeshFilter SecondaryWeaponMesh;

    [Header("Audio")] 
    [SerializeField] private AudioClip[] HitSounds;
    [SerializeField] private AudioClip[] SwingSounds;
    [SerializeField] private AudioClip SpellCast, SpellPrepare, BlockHit;

    [SerializeField] private SoundManager _manager = new();

    private bool mayAttack = true;

    void Start()
    {
        playerModel = new Creature("player1");
        currentMoveSpeed = baseMoveSpeed;

        // Инициализация дальнобойного оружия, если оно есть
        if (equippedWeapon != null && equippedWeapon is RangeWeapon)
        {
            UpdateRangedChargeDuration();
            CheckReloadOnEquip();
        }

        CrosshairStartPos = new Vector2[Crosshair.Length];
        Tweens = new TweenerCore<Vector3, Vector3, VectorOptions>[Crosshair.Length];
        for (int i = 0; i < Crosshair.Length; i++)
        {
            CrosshairStartPos[i] = Crosshair[i].position;
        }

        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.MeeleWeapon, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Shield, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.RangeWeapon, Rarity.Rare, true);

        Inventory.OnInventoryStateChange += value => mayAttack = !value;
    }

    public void SetWeapon(Weapon weapon)
    {
        equippedWeapon = weapon;
        WeaponMesh.mesh = weapon.Mesh;
        WeaponMesh.gameObject.SetActive(true);
        print(weapon.Effects.Count);

        animator.SetTrigger("StopCharge");
        animator.SetBool("IsCharging", false);
        animator.SetBool("IsCharged", false);
        animator.ResetTrigger("RangedShot");
        isRangedCharging = false;
        rangedChargeTime = 0f;
        CloseCrosshair();

        if (weapon is RangeWeapon rangeWeapon)
        {
            UpdateRangedChargeDuration();
            CheckReloadOnEquip(); // Проверяем и начинаем перезарядку, если нужно

            if (rangeWeapon.isReloaded)
            {
                OpenCrosshair(.25f);
            }
        }
    }

    public void SetSecondaryWeapon(SecondaryWeapon weapon)
    {
        secondaryWeapon = weapon;
        SecondaryWeaponMesh.mesh = weapon.Mesh;
        SecondaryWeaponMesh.gameObject.SetActive(true);
        print(weapon.Effects.Count);

        if (weapon is Shield)
            hasShield = true;
    }

    public void RemoveWeapon()
    {
        equippedWeapon = null;
        WeaponMesh.gameObject.SetActive(false);

        if (isReloading)
        {
            isReloading = false;
            reloadTimer = 0f;
            animator.SetBool("IsReloading", false);
            _manager.StopPlaying(SpellPrepare);
            ChargeSpellEffect.Stop();
        }

        if (isRangedCharging)
        {
            isRangedCharging = false;
            rangedChargeTime = 0f;
            animator.SetBool("IsCharging", false);
            animator.SetBool("IsCharged", false);
            animator.SetTrigger("StopCharge");
            _manager.StopPlaying(SpellPrepare);
        }

        CloseCrosshair(0f);
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
                case 0: direction = Vector2.up; break;
                case 1: direction = Vector2.right; break;
                case 2: direction = Vector2.down; break;
                case 3: direction = Vector2.left; break;
            }

            Tweens[i] = Crosshair[i].DOMove(CrosshairStartPos[i] + direction * 5f, time);
        }

        Tweens[0].onComplete += () => Cross.DOScale(Vector3.one, .25f);
    }

    private void CloseCrosshair(float duration = 0.25f)
    {
        ChargeSpellEffect.Stop();
        Cross.DOScale(Vector3.zero, duration);
        if (duration > 0)
        {
            for (int i = 0; i < Crosshair.Length; i++)
            {
                Tweens[i]?.Kill();
                Tweens[i] = Crosshair[i].DOMove(CrosshairStartPos[i], 0.25f);
            }
        }
        else
        {
            for (int i = 0; i < Crosshair.Length; i++)
            {
                Tweens[i]?.Kill();
                Crosshair[i].transform.position = CrosshairStartPos[i];
            }
        }
            
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Shield, Rarity.Legendary);

        playerModel.Update(Time.deltaTime);

        if (currentAttackIndex > 0 && Time.time - lastAttackTime > comboWindow)
        {
            ResetAttackCombo();
        }

        if (!mayAttack)
            return;

        // Обработка перезарядки
        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0)
            {
                EndReload();
            }
            return; // Перезарядка не прерывается
        }

        // Ближний бой (ЛКМ)
        if (Input.GetMouseButtonDown(0) && !isBlocking && Time.time - lastAttackTime >= attackCooldown)
        {
            if (equippedWeapon != null && equippedWeapon is MeeleWeapon)
            {
                PerformAttack(); // Ближний бой
            }
        }

        // Дальнобойное оружие (ЛКМ для выстрела или зарядки)
        if (equippedWeapon != null && equippedWeapon is RangeWeapon rangeWeapon)
        {
            if (Input.GetMouseButtonDown(0) && !isBlocking && Time.time - lastAttackTime >= attackCooldown)
            {
                if (rangeWeapon.isReloadable)
                {
                    if (rangeWeapon.isReloaded)
                    {
                        PerformReloadableRangedAttack(); // Выстрел для перезаряжаемого оружия
                    }
                }
                else if (!isRangedCharging)
                {
                    StartRangedCharge();
                }
            }
            else if (Input.GetKey(KeyCode.Mouse0) && isRangedCharging && !rangeWeapon.isReloadable)
            {
                UpdateRangedCharge();
            }
            else if (Input.GetKeyUp(KeyCode.Mouse0) && isRangedCharging && !rangeWeapon.isReloadable)
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
            case 1: animator.SetTrigger("Attack1"); break;
            case 2: animator.SetTrigger("Attack2"); break;
            case 3: animator.SetTrigger("Attack3"); break;
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
        
        _manager.ProduceSound(transform.position, SwingSounds[Random.Range(0, SwingSounds.Length)]);
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
        UpdateRangedChargeDuration();
        _manager.ProduceSound(transform.position, SpellPrepare, true);
        OpenCrosshair(equippedWeapon[StatType.AttackSpeed].CurrentValue);
    }

    private void UpdateRangedCharge()
    {
        rangedChargeTime += Time.deltaTime * (1 / equippedWeapon.Stats[StatType.AttackSpeed].CurrentValue);
        if (rangedChargeTime >= rangedChargeDuration)
        {
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
            animator.SetTrigger("RangedShot");
            FireProjectile();
            _manager.ProduceSound(transform.position, SpellCast);
        }
        else // Частичный заряд
        {
            animator.SetTrigger("StopCharge");
            Debug.Log("Shot cancelled or partial charge!");
        }
        _manager.StopPlaying(SpellPrepare);
    }

    private void PerformReloadableRangedAttack()
    {
        RangeWeapon rangeWeapon = (RangeWeapon)equippedWeapon;
        animator.SetTrigger("RangedShot");
        FireProjectile();
        _manager.ProduceSound(transform.position, SpellCast);

        lastAttackTime = Time.time;
        rangeWeapon.isReloaded = false; // Сбрасываем состояние заряженности
        CloseCrosshair(0);
        UniTask.Run(async () =>
        {
            await UniTask.Delay(1000);
            StartReload(); // Начинаем перезарядку
        });
    }

    private void StartReload()
    {
        RangeWeapon rangeWeapon = (RangeWeapon)equippedWeapon;
        isReloading = true;
        reloadTimer = 1f / rangeWeapon.Stats[StatType.AttackSpeed].CurrentValue; // Время перезарядки зависит от AttackSpeed
        animator.SetBool("IsReloading", true); // Предполагается, что есть анимация перезарядки
        _manager.ProduceSound(transform.position, SpellPrepare, true); // Звук перезарядки
        OpenCrosshair(equippedWeapon[StatType.AttackSpeed].CurrentValue);
    }

    private void EndReload()
    {
        RangeWeapon rangeWeapon = (RangeWeapon)equippedWeapon;
        isReloading = false;
        rangeWeapon.isReloaded = true; // Оружие заряжено
        animator.SetBool("IsReloading", false);
        _manager.StopPlaying(SpellPrepare);
        Debug.Log("Weapon reloaded!");
        
        if(rangeWeapon.isReloadable)
            ChargeSpellEffect.Stop();
    }

    private void CheckReloadOnEquip()
    {
        if (equippedWeapon is RangeWeapon rangeWeapon && rangeWeapon.isReloadable && !rangeWeapon.isReloaded)
        {
            StartReload(); // Автоматическая перезарядка при установке незаряженного оружия
        }
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
            rangedChargeDuration = 1f; // Фиксированное время зарядки
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
            Debug.Log("Player blocked an attack!");
            _manager.ProduceSound(transform.position, BlockHit);
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
            playerModel.Stats[StatType.Health].Modify(-value / 3);
        }
        else
        {
            playerModel.Stats[StatType.Health].Modify(-value);
        }
        _manager.ProduceSound(transform.position, HitSounds[Random.Range(0, HitSounds.Length)]);
    }
}