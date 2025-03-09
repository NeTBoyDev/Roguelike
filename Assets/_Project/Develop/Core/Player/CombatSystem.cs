using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CombatSystem : MonoBehaviour
{
    public Creature playerModel { get; private set; }
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

    private Artifact artifact1;
    private Artifact artifact2;

    private Vector3 moveInput;

    public RectTransform[] Crosshair;
    public RectTransform Cross;
    public Vector2[] CrosshairStartPos;
    public TweenerCore<Vector3, Vector3, VectorOptions>[] Tweens;

    [Space] [Header("Effects")] 
    public ParticleSystem ChargeSpellEffect;
    
    public Transform WeaponParent;
    public Transform SecondaryWeaponParent;

    private GameObject WeaponView;
    private GameObject SecondaryWeaponView;

    [Header("Audio")] 
    [SerializeField] private AudioClip[] HitSounds;
    [SerializeField] private AudioClip[] SwingSounds;
    [SerializeField] private AudioClip SpellCast, SpellPrepare, BlockHit,Reload,Shot,DieClip,Equip;

    [SerializeField] private SoundManager _manager = new();

    private bool mayAttack = true;

    private PlayerCharacter character;

    public Slider HpSlider;
    public Slider StaminaSlider;
    public Image HitImage;
    public CanvasGroup DeadScreen;
    public CanvasGroup CrossScreen;
    public CanvasGroup MinimapScreen;
    public CanvasGroup UiScreen;

    private void Awake()
    {
        playerModel = new Creature("player1");
    }

    void Start()
    {
        
        character = GetComponent<PlayerCharacter>();
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

        /*ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Map, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Dagger, Rarity.Common);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Crossbow, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Crossbow, Rarity.Common);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Gem, Rarity.Common);*/
        /*ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Sword, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Hammer, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Axe, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Shield, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Crossbow, Rarity.Rare, false);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.Staff, Rarity.Rare, false);*/

        Inventory.OnInventoryStateChange += value => mayAttack = !value;
        
        InitializeStats(GameData._preset);
    }

    public void InitializeStats(StatPreset preset)
    {
        foreach (var stat in playerModel.Stats)
        {
            print(stat.Key);
            stat.Value.SetValue(preset.Stats.First(s => s.Type == stat.Key).CurrentValue,preset.Stats.First(s => s.Type == stat.Key).MaxValue);
        }
        
        playerModel.Stats[StatType.Stamina] = new Stat(StatType.Stamina, preset.Stats.First(s=>s.Type == StatType.Stamina).BaseValue, preset.Stats.First(s=>s.Type == StatType.Stamina).MaxValue);
        playerModel.Stats[StatType.Health] = new Stat(StatType.Health, preset.Stats.First(s=>s.Type == StatType.Health).BaseValue, preset.Stats.First(s=>s.Type == StatType.Health).MaxValue);
       
        
        var agility = playerModel.Stats[StatType.Agility];
        agility.OnModify += (value) => character.SetSpeed(3 + value/5);
        
        var health = playerModel.Stats[StatType.Health];
        print(health.BaseValue);
        health.OnModify += (value) => HpSlider.value = value;
        HpSlider.maxValue = health.BaseValue;
        
        var stamina = playerModel.Stats[StatType.Stamina];
        stamina.OnModify += (value) => StaminaSlider.value = value;
        StaminaSlider.maxValue = stamina.BaseValue;
        print(stamina.BaseValue);
        
        
    }

    private void RegenStats()
    {
        playerModel.Stats[StatType.Health].Modify(Time.deltaTime);
        playerModel.Stats[StatType.Stamina].Modify(Time.deltaTime * 10);
    }

    public void SetWeapon(Weapon weapon)
    {
        _manager.StopPlaying(Reload);
        if(equippedWeapon!= null)
            RemoveWeapon();
        equippedWeapon = weapon;
        WeaponView = Instantiate(weapon.View, WeaponParent);
        WeaponView.transform.localPosition = Vector3.zero;
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

        ClearWeaponBooleans();
        
        if(weapon is Sword)
            animator.SetBool("Sword",true);
        if(weapon is Dagger)
            animator.SetBool("Dagger",true);
        if(weapon is Hammer)
            animator.SetBool("Hammer",true);
        if(weapon is Axe)
            animator.SetBool("Axe",true);

        attackCooldown = 0.5f / equippedWeapon[StatType.AttackSpeed].CurrentValue;
        animator.SetFloat("AttackSpeed",equippedWeapon is MeeleWeapon ? equippedWeapon[StatType.AttackSpeed].CurrentValue 
            : 1f + (1 -equippedWeapon.Stats[StatType.RangeAttackSpeed].CurrentValue));
        _manager.ProduceSound(transform.position,Equip);
    }

    private void ClearWeaponBooleans()
    {
        animator.SetBool("Sword",false);
        animator.SetBool("Axe",false);
        animator.SetBool("Dagger",false);
        animator.SetBool("Hammer",false);
    }

    public void SetSecondaryWeapon(SecondaryWeapon weapon)
    {
        if(secondaryWeapon!= null)
            RemoveSecondaryWeapon();
        secondaryWeapon = weapon;
        SecondaryWeaponView = Instantiate(weapon.View, SecondaryWeaponParent);
        SecondaryWeaponView.transform.localPosition = Vector3.zero;
        print(weapon.Effects.Count);
        
        foreach (var stat in weapon.Stats)
        {
            if(playerModel.Stats.ContainsKey(stat.Key))
                playerModel.Stats[stat.Key].Modify(stat.Value.CurrentValue);
        }

        if (weapon is Shield)
            hasShield = true;
        _manager.ProduceSound(transform.position,Equip);
    }

    public void RemoveWeapon()
    {
        equippedWeapon = null;
        Destroy(WeaponView);

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
            _manager.StopPlaying(Reload);
        }

        CloseCrosshair(0f);
    }

    public void RemoveSecondaryWeapon()
    {
        foreach (var stat in secondaryWeapon.Stats)
        {
            if(playerModel.Stats.ContainsKey(stat.Key))
                playerModel.Stats[stat.Key].Modify(-stat.Value.CurrentValue);
           
        }
        
        secondaryWeapon = null;
        Destroy(SecondaryWeaponView);
        hasShield = false;
    }
    
    public void SetFirstArtifact(Artifact weapon)
    {
        if(artifact1!= null)
            RemoveFirstArtifact();
        artifact1 = weapon;
        foreach (var stat in weapon.Stats)
        {
            playerModel.Stats[stat.Key].Modify(stat.Value.CurrentValue);
        }
        _manager.ProduceSound(transform.position,Equip);
    }
    public void RemoveFirstArtifact()
    {
        foreach (var stat in artifact1.Stats)
        {
            playerModel.Stats[stat.Key].Modify(-stat.Value.CurrentValue);
        }
        artifact1 = null;
    }
    
    public void SetSecondArtifact(Artifact weapon)
    {
        if(artifact2!= null)
            RemoveSecondArtifact();
        artifact2 = weapon;
        foreach (var stat in weapon.Stats)
        {
            playerModel.Stats[stat.Key].Modify(stat.Value.CurrentValue);
        }
        _manager.ProduceSound(transform.position,Equip);
    }
    public void RemoveSecondArtifact()
    {
        foreach (var stat in artifact2.Stats)
        {
            playerModel.Stats[stat.Key].Modify(-stat.Value.CurrentValue);
        }
        artifact2 = null;
    }

    private void OpenCrosshair(float time)
    {
        
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

        if (!mayAttack)
            return;
        
        // Ближний бой (ЛКМ)
        if (Input.GetMouseButtonDown(0) && !isBlocking && Time.time - lastAttackTime >= attackCooldown)
        {
            if (equippedWeapon != null && equippedWeapon is MeeleWeapon 
                                       && EnoughStamina)
            {
                PerformAttack(); // Ближний бой
            }
        }

        // Дальнобойное оружие (ЛКМ для выстрела или зарядки)
        if (equippedWeapon != null && equippedWeapon is RangeWeapon rangeWeapon)
        {
            if (Input.GetMouseButtonDown(0) && !isBlocking && Time.time - lastAttackTime >= attackCooldown && EnoughStamina)
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
            else if (Input.GetKeyUp(KeyCode.Mouse0) && isRangedCharging && !rangeWeapon.isReloadable && EnoughStamina)
            {
                PerformRangedAttack(); //Атака магией
                CloseCrosshair();
            }
        }

        if (hasShield) // Блок для ближнего боя
        {
            if (Input.GetMouseButtonDown(1) && !isBlocking && playerModel.Stats[StatType.Stamina].CurrentValue > secondaryWeapon.Stats[StatType.StaminaCost].CurrentValue)
            {
                StartBlock();
            }
            else if (Input.GetMouseButtonUp(1) && isBlocking || playerModel.Stats[StatType.Stamina].CurrentValue <
                     secondaryWeapon.Stats[StatType.StaminaCost].CurrentValue)
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

    private bool EnoughStamina => playerModel.Stats[StatType.Stamina].CurrentValue >
                                  equippedWeapon.Stats[StatType.StaminaCost].CurrentValue;

    private void LateUpdate()
    {
        RegenStats();
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

        
    }

    public void HandleAttack() //Для вызова из анимаций
    {
        Vector3 attackDirection = transform.forward;
        Vector3 attackPoint = transform.position + Vector3.up + attackDirection * attackRange * 0.5f;

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint, equippedWeapon[StatType.AttackRange].CurrentValue);
        foreach (var hit in hitEnemies)
        {
            if (hit.CompareTag("Enemy"))
            {
                AIBase enemy = hit.GetComponent<AIBase>();
                if (enemy != null)
                {
                    float damage = (playerModel[StatType.Strength].CurrentValue / 10 + 1) * equippedWeapon[StatType.Damage].CurrentValue;
                    enemy.TakeDamage(damage);
                    ((MeeleWeapon)equippedWeapon).ApplyEffects(enemy.skeletonModel);
                    Debug.Log($"Player attacked {hit.name} for {damage} damage!");
                }
            }
        }
        ((MeeleWeapon)equippedWeapon).FireProjectile(1 + playerModel[StatType.Strength].CurrentValue/10);
        
        _manager.ProduceSound(transform.position, SwingSounds[Random.Range(0, SwingSounds.Length)]);
        
        playerModel.Stats[StatType.Stamina].Modify(-equippedWeapon.Stats[StatType.StaminaCost].CurrentValue);
    } 

    private void OnDrawGizmos()
    {
        if (equippedWeapon != null)
        {
            Vector3 attackDirection = transform.forward;
            Vector3 attackPoint = transform.position + Vector3.up + attackDirection * attackRange * 0.5f;
            Gizmos.DrawSphere(attackPoint, equippedWeapon[StatType.AttackRange].CurrentValue);
        }
        
    }

    private void StartRangedCharge()
    {
        isRangedCharging = true;
        rangedChargeTime = 0f;
        animator.SetBool("IsCharging", true);
        UpdateRangedChargeDuration();
        _manager.ProduceSound(transform.position, SpellPrepare, true);
        ChargeSpellEffect.Play();
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
            
            FireProjectile(1 + playerModel[StatType.Intelligence].CurrentValue/10);
            _manager.ProduceSound(transform.position, SpellCast);
        }
        else // Частичный заряд
        {
            animator.SetTrigger("StopCharge");
            Debug.Log("Shot cancelled or partial charge!");
        }
        _manager.StopPlaying(SpellPrepare);
        
        playerModel.Stats[StatType.Stamina].Modify(-equippedWeapon.Stats[StatType.StaminaCost].CurrentValue);
    }

    private void PerformReloadableRangedAttack()
    {
        RangeWeapon rangeWeapon = (RangeWeapon)equippedWeapon;
        animator.SetTrigger("RangedShot");
        FireProjectile(1 + playerModel[StatType.Agility].CurrentValue/10);
        
        _manager.ProduceSound(transform.position, Shot);

        lastAttackTime = Time.time;
        rangeWeapon.isReloaded = false; // Сбрасываем состояние заряженности
        CloseCrosshair(0);
        UniTask.Run(async () =>
        {
            await UniTask.Delay(1000);
            StartReload(); // Начинаем перезарядку
        });
        
        playerModel.Stats[StatType.Stamina].Modify(-equippedWeapon.Stats[StatType.StaminaCost].CurrentValue);
    }

    private void StartReload()
    {
        CloseCrosshair(0);
        RangeWeapon rangeWeapon = (RangeWeapon)equippedWeapon;
        isReloading = true;
        reloadTimer = 1f / (1 + (1-rangeWeapon.Stats[StatType.RangeAttackSpeed].CurrentValue)); // Время перезарядки зависит от AttackSpeed
        animator.SetBool("IsReloading", true); // Предполагается, что есть анимация перезарядки
        animator.Play("Reload");
        _manager.ProduceSound(transform.position, Reload, true); // Звук перезарядки
        OpenCrosshair(reloadTimer);
    }

    private void EndReload()
    {
        RangeWeapon rangeWeapon = (RangeWeapon)equippedWeapon;
        isReloading = false;
        rangeWeapon.isReloaded = true; // Оружие заряжено
        animator.SetBool("IsReloading", false);
        animator.ResetTrigger("RangedShot");
        _manager.StopPlaying(Reload);
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

    private void FireProjectile(float multiplyier)
    {
        ((RangeWeapon)equippedWeapon).FireProjectile(multiplyier);
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
            playerModel.Stats[StatType.Stamina].Modify(-secondaryWeapon.Stats[StatType.StaminaCost].CurrentValue);
                
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
            if (playerModel.Stats[StatType.Health].CurrentValue - value / 3 <= 0)
            {
                Die();
                return;
            }
            
            OnHitWhileBlocking();
            playerModel.Stats[StatType.Health].Modify(-value / 3);
        }
        else
        {
            if (playerModel.Stats[StatType.Health].CurrentValue - value <= 0)
            {
                Die();
                return;
            }
            playerModel.Stats[StatType.Health].Modify(-value);
        }
        
        _manager.ProduceSound(transform.position, HitSounds[Random.Range(0, HitSounds.Length)]);
        HitEffect();
    }

    private void HitEffect()
    {
        HitImage.color = new Color(1, 1, 1, 1);
        DOTween.To(() => HitImage.color, x => HitImage.color = x, new Color(1, 1, 1, 0), .75f);
    }

    private void Die()
    {
        Cursor.lockState = CursorLockMode.None;
        
        var camera = FindObjectOfType<PlayerCamera>();
        camera.mayUpdate = false;
        camera.transform.DOJump(-camera.transform.forward, 1, 1, .75f).SetRelative();
        camera.transform.DOLocalRotate(new Vector3(-75,0,-75), 1).SetEase(Ease.OutBounce);
        DOTween.To(() => HitImage.color, x => HitImage.color = x, new Color(1, 1, 1, 1), .75f).SetUpdate(true);
        DOTween.To(() => DeadScreen.alpha, x => DeadScreen.alpha = x, 1, .75f).SetUpdate(true);
        DOTween.To(() => CrossScreen.alpha, x => CrossScreen.alpha = x, 0, .75f).SetUpdate(true);
        DOTween.To(() => UiScreen.alpha, x => UiScreen.alpha = x, 0, .75f).SetUpdate(true);
        DOTween.To(() => MinimapScreen.alpha, x => MinimapScreen.alpha = x, 0, .75f).SetUpdate(true);
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 0, 3f).SetUpdate(true);
        
        _manager.StopPlaying(Reload);
        
        _manager.ProduceSound(transform.position,DieClip);
        
        OnDie?.Invoke();
    }

    public static event Action OnDie;
}