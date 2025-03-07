using _Project.Develop.Core.Enum;
using _Project.Develop.Core.Player;
using UnityEngine;
using Pathfinding;
using Random = UnityEngine.Random;

public enum MageSkeletonState
{
    KeepDistance,   // Поддержание дистанции
    Retreat,        // Отступление от игрока
    ChargeSpell,    // Зарядка заклинания
    CastSpell,      // Выстрел заклинанием
    TakeDamage,     // Получение урона
    Dead            // Смерть
}

public class MageSkeletonAI : AIBase
{
    public AIDestinationSetter destinationSetter;
    private AIPath aiPath;

    private Transform target;

    [SerializeField] private float castRange = 5f;          // Дальность атаки заклинанием
    [SerializeField] private float keepDistanceRange = 4f;  // Предпочитаемая дистанция до игрока
    [SerializeField] private float retreatDistance = 6f;    // Дистанция для отступления
    [SerializeField] private float chargeTime = 3f;       // Время зарядки заклинания
    [SerializeField] private float castCooldown = 2f;       // Кулдаун между заклинаниями
    [SerializeField] public float retreatChance = 0.7f;    // Шанс отступления при сближении (70%)
    private float lastCastTime;

    private IState currentState;
    public Animator animator;
    public ParticleSystem hitEffect;
    public ParticleSystem castEffect; // Эффект заклинания
    public AudioClip hitClip;
    public AudioClip castClip;
    public AudioClip shotClip;

    public SoundManager soundManager = new();

    void Start()
    {
        skeletonModel = new Creature("skeleton1");
        destinationSetter = GetComponent<AIDestinationSetter>();
        aiPath = GetComponent<AIPath>();
        
        target = GameObject.FindGameObjectWithTag("Player").transform;
        destinationSetter.target = target;

        animator = GetComponent<Animator>();
        hitEffect = GetComponentInChildren<ParticleSystem>();
        castEffect = GetComponentInChildren<ParticleSystem>(); // Предполагается, что у вас есть такой эффект

        ChangeState(new MageKeepDistanceState(this));
    }

    void Update()
    {
        skeletonModel.Update(Time.deltaTime);

        currentState?.Execute();

        if (skeletonModel.Stats[StatType.Health].CurrentValue <= 0 && currentState is not MageDeadState)
        {
            ChangeState(new MageDeadState(this));
            GetComponent<Collider>().enabled = false;
        }
    }

    public void ChangeState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public override void TakeDamage(float damage)
    {
        if (!(currentState is DeadState))
        {
            if (currentState is ChargeSpellState chargeState)
            {
                chargeState.Interrupt(); // Прерываем зарядку
            }
            else if (currentState is CastSpellState castState)
            {
                castState.Interrupt(); // Прерываем каст
            }
            ChangeState(new MageTakeDamageState(this));
            hitEffect.Play();
            soundManager.ProduceSound(transform.position, hitClip);
            base.TakeDamage(damage);
        }
    }

    [SerializeField] private EnemyProjectile spellPrefab;

    public void CastSpell()
    {
        castEffect.Play();
        
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        Vector3 spellPoint = transform.position + Vector3.up + directionToTarget * castRange * 0.5f;
        
            if (target != null)
            {
                float damage = skeletonModel[StatType.Strength].CurrentValue * 1.5f; 
                var direction = target.transform.position - transform.position;
                var rotation = Quaternion.LookRotation(directionToTarget, transform.up);
                animator.Play("Spellcast_Shoot");
                
                var projectile = Object.Instantiate(spellPrefab, 
                    spellPoint, 
                    rotation);
        
                projectile.SetDamage(damage);
                soundManager.ProduceSound(transform.position,shotClip);
                
            }
        
    }

    public float DistanceToTarget => Vector3.Distance(transform.position, target.position);
    public float CastRange => castRange;
    public float KeepDistanceRange => keepDistanceRange;
    public float RetreatDistance => retreatDistance;
    public float ChargeTime => chargeTime;
    public float CastCooldown => castCooldown;
    public float LastCastTime { get => lastCastTime; set => lastCastTime = value; }
    public Transform Target => target;
    public AIPath AIPath => aiPath;
}

public class MageKeepDistanceState : IState
{
    private MageSkeletonAI skeleton;

    public MageKeepDistanceState(MageSkeletonAI skeleton)
    {
        this.skeleton = skeleton;
    }

    public void Enter()
    {
        skeleton.animator.SetBool("Walking", true);
        skeleton.AIPath.canMove = true;
        skeleton.AIPath.maxSpeed = 2.5f;
    }

    public void Execute()
    {
        float distance = skeleton.DistanceToTarget;

        if (distance <= skeleton.CastRange && Time.time - skeleton.LastCastTime >= skeleton.CastCooldown)
        {
            skeleton.ChangeState(new ChargeSpellState(skeleton));
        }
        else if (distance < skeleton.KeepDistanceRange - 0.5f && Random.value <= skeleton.retreatChance)
        {
            skeleton.ChangeState(new MageRetreatState(skeleton));
        }
        else if (distance > skeleton.KeepDistanceRange + 0.5f)
        {
            skeleton.AIPath.destination = skeleton.Target.position;
        }
        else
        {
            skeleton.AIPath.destination = skeleton.transform.position; // Останавливаемся на месте
        }
    }

    public void Exit()
    {
        skeleton.animator.SetBool("Walking", false);
        skeleton.AIPath.canMove = false;
    }
}

public class MageRetreatState : IState
{
    private MageSkeletonAI skeleton;

    public MageRetreatState(MageSkeletonAI skeleton)
    {
        this.skeleton = skeleton;
    }

    public void Enter()
    {
        skeleton.animator.SetBool("Walking", true);
        skeleton.AIPath.canMove = true;
        skeleton.AIPath.maxSpeed = 3f; // Быстрее отступаем
    }

    public void Execute()
    {
        float distance = skeleton.DistanceToTarget;
        Vector3 retreatDirection = (skeleton.transform.position - skeleton.Target.position).normalized;
        Vector3 retreatPosition = skeleton.transform.position + retreatDirection * skeleton.RetreatDistance;

        skeleton.destinationSetter.target = null; // Отключаем стандартную цель
        skeleton.AIPath.destination = retreatPosition;

        if (distance >= skeleton.RetreatDistance)
        {
            skeleton.ChangeState(new MageKeepDistanceState(skeleton));
        }
    }

    public void Exit()
    {
        skeleton.animator.SetBool("Walking", false);
        skeleton.AIPath.canMove = false;
        skeleton.destinationSetter.target = skeleton.Target;
    }
}

public class ChargeSpellState : IState
{
    private MageSkeletonAI skeleton;
    private float chargeTimer;
    private bool isInterrupted;

    public ChargeSpellState(MageSkeletonAI skeleton)
    {
        this.skeleton = skeleton;
    }

    public void Enter()
    {
        skeleton.AIPath.canMove = false;
        skeleton.animator.SetTrigger("IsCharging");
        chargeTimer = skeleton.ChargeTime;
        isInterrupted = false;
        skeleton.soundManager.ProduceSound(skeleton.transform.position, skeleton.castClip,true);
    }

    public void Execute()
    {
        if (isInterrupted)
        {
            skeleton.ChangeState(new MageKeepDistanceState(skeleton));
            return;
        }

        chargeTimer -= Time.deltaTime;
        if (chargeTimer <= 0)
        {
            skeleton.ChangeState(new CastSpellState(skeleton));
        }
    }

    public void Exit()
    {
        skeleton.animator.ResetTrigger("IsCharging");
        skeleton.AIPath.canMove = true;
        skeleton.soundManager.StopPlaying(skeleton.castClip);
    }

    public void Interrupt()
    {
        isInterrupted = true;
    }
}

public class CastSpellState : IState
{
    private MageSkeletonAI skeleton;
    private bool isInterrupted;

    public CastSpellState(MageSkeletonAI skeleton)
    {
        this.skeleton = skeleton;
    }

    public void Enter()
    {
        skeleton.AIPath.canMove = false;
        skeleton.animator.SetTrigger("CastSpell");
        skeleton.CastSpell();
        skeleton.LastCastTime = Time.time;
        isInterrupted = false;
    }

    public void Execute()
    {
        AnimatorStateInfo stateInfo = skeleton.animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Spellcast_Shoot") && stateInfo.normalizedTime >= 1f && !isInterrupted)
        {
            skeleton.ChangeState(new MageKeepDistanceState(skeleton));
        }
        else if (isInterrupted)
        {
            skeleton.ChangeState(new MageKeepDistanceState(skeleton));
        }
    }

    public void Exit()
    {
        skeleton.AIPath.canMove = true;
    }

    public void Interrupt()
    {
        isInterrupted = true;
    }
}

public class MageTakeDamageState : IState
{
    private MageSkeletonAI skeleton;
    private float staggerDuration = 1f;
    private float staggerTimer;
    private bool stun;

    public MageTakeDamageState(MageSkeletonAI skeleton)
    {
        this.skeleton = skeleton;
        stun = Random.value > 0.2f; // 80% шанс стана
        if (stun)
            Debug.Log("Mage Skeleton STUNNED");
    }

    public void Enter()
    {
        skeleton.animator.SetTrigger("Hit");
        skeleton.AIPath.canMove = false;
        staggerTimer = staggerDuration;
    }

    public void Execute()
    {
        staggerTimer -= Time.deltaTime;
        if (staggerTimer <= 0 || !stun)
        {
            skeleton.ChangeState(new MageKeepDistanceState(skeleton));
        }
    }

    public void Exit()
    {
        skeleton.AIPath.canMove = true;
    }
}

public class MageDeadState : IState
{
    private MageSkeletonAI skeleton;

    public MageDeadState(MageSkeletonAI skeleton)
    {
        this.skeleton = skeleton;
    }

    public void Enter()
    {
        skeleton.animator.Play("Death_A");
        skeleton.animator.SetTrigger($"Die");
        skeleton.animator.SetBool($"isDead",true);
        skeleton.AIPath.canMove = false;
        Debug.Log("Skeleton is dead!");
    }

    public void Execute() { }

    public void Exit() { }
}