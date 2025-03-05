using _Project.Develop.Core.Enum;
using UnityEngine;
using Pathfinding; 

public enum SkeletonState
{
    Approach,       // Приближение к цели
    Retreat,        // Отдаление от цели
    KeepDistance,   // Поддержание малой дистанции
    Attack,         // Атака
    TakeDamage,     // Получение урона
    Dead            // Смерть
}

public class SkeletonAI : MonoBehaviour
{
    public AIDestinationSetter destinationSetter;
    private AIPath aiPath;

    public Creature skeletonModel { get; private set; }

    private Transform target;

    [SerializeField] private float attackRange = 1.5f;         
    [SerializeField] private float keepDistanceRange = 3f;     
    [SerializeField] private float retreatDistance = 5f;       
    [SerializeField] private float attackCooldown = 1f;        
    private float lastAttackTime;

    private IState currentState;
    public Animator animator;
    public ParticleSystem hitEffect;

    void Start()
    {
        destinationSetter = GetComponent<AIDestinationSetter>();
        aiPath = GetComponent<AIPath>();
        
        skeletonModel = new Creature("skeleton1");
        
        target = GameObject.FindGameObjectWithTag("Player").transform;
        destinationSetter.target = target;

        animator = GetComponent<Animator>();
        hitEffect = GetComponentInChildren<ParticleSystem>();

        ChangeState(new ApproachState(this));
    }

    void Update()
    {
        skeletonModel.Update(Time.deltaTime);
        //print(skeletonModel.Effects.Count);

        currentState?.Execute();

        if (skeletonModel.Stats[StatType.Health].CurrentValue <= 0 && currentState is not DeadState)
        {
            ChangeState(new DeadState(this));
            GetComponent<Collider>().enabled = false;
        }
    }

    public void ChangeState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void TakeDamage(float damage)
    {
        if (!(currentState is DeadState))
        {
            skeletonModel.Stats[StatType.Health].Modify(-damage);
            if (currentState is AttackState attackState)
            {
                attackState.Interrupt(); // Прерываем текущее состояние атаки
            }
            ChangeState(new TakeDamageState(this));
        }
    }

    public float DistanceToTarget => Vector3.Distance(transform.position, target.position);

    public float AttackRange => attackRange;
    public float KeepDistanceRange => keepDistanceRange;
    public float RetreatDistance => retreatDistance;
    public float AttackCooldown => attackCooldown;
    public float LastAttackTime { get => lastAttackTime; set => lastAttackTime = value; }
    public Transform Target => target;
    public AIPath AIPath => aiPath;
}

public interface IState
{
    void Enter();
    void Execute();
    void Exit();
}

public class ApproachState : IState
{
    private SkeletonAI skeleton;

    public ApproachState(SkeletonAI skeleton)
    {
        this.skeleton = skeleton;
    }

    public void Enter()
    {
        skeleton.animator.SetBool("Walking",true);
        skeleton.AIPath.canMove = true;
        skeleton.AIPath.maxSpeed = 3f; // Скорость приближения
    }

    public void Execute()
    {
        float distance = skeleton.DistanceToTarget;
        if (distance <= skeleton.AttackRange)
        {
            skeleton.ChangeState(new AttackState(skeleton));
        }/*
        else if (distance > skeleton.RetreatDistance)
        {
            skeleton.ChangeState(new RetreatState(skeleton));
        }*/
    }

    public void Exit()
    {
        skeleton.AIPath.canMove = false;
        skeleton.animator.SetBool("Walking",false);
    }
}

public class RetreatState : IState
{
    private SkeletonAI skeleton;

    public RetreatState(SkeletonAI skeleton)
    {
        this.skeleton = skeleton;
    }

    public void Enter()
    {
        skeleton.animator.SetBool($"Walking",true);
        skeleton.AIPath.canMove = true;
        skeleton.AIPath.maxSpeed = 2f; 
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
            skeleton.ChangeState(new KeepDistanceState(skeleton));
        }
    }

    public void Exit()
    {
        skeleton.animator.SetBool($"Walking",false);
        skeleton.AIPath.canMove = false;
        skeleton.destinationSetter.target = skeleton.Target; 
    }
}

public class KeepDistanceState : IState
{
    private SkeletonAI skeleton;

    public KeepDistanceState(SkeletonAI skeleton)
    {
        this.skeleton = skeleton;
    }

    public void Enter()
    {
        skeleton.animator.SetBool($"Walking",true);
        skeleton.AIPath.canMove = true;
        skeleton.AIPath.maxSpeed = 2.5f;
    }

    public void Execute()
    {
        float distance = skeleton.DistanceToTarget;
        if (distance < skeleton.AttackRange)
        {
            skeleton.ChangeState(new AttackState(skeleton));
        }
        else if (distance > skeleton.KeepDistanceRange + 0.5f)
        {
            skeleton.ChangeState(new ApproachState(skeleton));
        }
        else if (distance < skeleton.KeepDistanceRange - 0.5f)
        {
            skeleton.ChangeState(new RetreatState(skeleton));
        }
    }

    public void Exit()
    {
        skeleton.animator.SetBool($"Walking",false);
        skeleton.AIPath.canMove = false;
    }
}

public class AttackState : IState
{
    private SkeletonAI skeleton;
    private bool isAttackCooldown; // Флаг кулдауна после атаки
    private float attackCooldownTimer; // Таймер кулдауна
    private bool isAnimationPlaying; // Флаг, показывающий, что анимация атаки ещё идёт
    private bool isInterrupted; // Флаг прерывания (например, получение урона)

    public AttackState(SkeletonAI skeleton)
    {
        this.skeleton = skeleton;
    }

    public void Enter()
    {
        skeleton.AIPath.canMove = false;
        isAttackCooldown = false;
        attackCooldownTimer = 0f;
        isAnimationPlaying = true; // Анимация начинается
        isInterrupted = false; // Сбрасываем флаг прерывания
    }

    public void Execute()
    {
        float distance = skeleton.DistanceToTarget;

        // Проверяем, завершена ли анимация
        AnimatorStateInfo stateInfo = skeleton.animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Attack1") || stateInfo.IsName("Attack2") || stateInfo.IsName("Attack3"))
        {
            if (stateInfo.normalizedTime >= 1f)
            {
                isAnimationPlaying = false; // Анимация завершена
            }
        }
        else
        {
            isAnimationPlaying = false; // Если мы не в состоянии атаки, считаем анимацию завершённой
        }

        // Если анимация прервана (например, уроном), выходим сразу
        if (isInterrupted)
        {
            ChooseNextState();
            return;
        }

        // Если кулдаун активен, ждём его окончания
        if (isAttackCooldown)
        {
            attackCooldownTimer -= Time.deltaTime;
            if (attackCooldownTimer <= 0 && !isAnimationPlaying)
            {
                isAttackCooldown = false;
                ChooseNextState(); // Выходим только после завершения анимации и кулдауна
            }
            return;
        }

        // Проверяем, можем ли атаковать
        if (Time.time - skeleton.LastAttackTime >= skeleton.AttackCooldown && !isAnimationPlaying)
        {
            PerformAttack();
            StartAttackCooldown();
        }
    }

    private void PerformAttack()
    {
        Vector3 attackDirection = skeleton.transform.forward;
        Vector3 attackPoint = skeleton.transform.position + Vector3.up + attackDirection * skeleton.AttackRange * 0.5f;

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint, skeleton.AttackRange * 0.5f);
        foreach (var hit in hitEnemies)
        {
            if (hit.CompareTag("Player"))
            {
                if (hit.TryGetComponent(out CombatSystem enemy))
                {
                    float damage = skeleton.skeletonModel[StatType.Strength].CurrentValue;
                    enemy.TakeDamage(damage);
                    Debug.Log($"Skeleton attacked {hit.name} for {damage} damage!");
                }
            }
        }

        skeleton.LastAttackTime = Time.time;
        isAnimationPlaying = true; // Запускаем новую анимацию
        skeleton.animator.Play($"Attack{Random.Range(1, 4)}");
        Debug.Log("Skeleton attacks!");
    }

    private void StartAttackCooldown()
    {
        isAttackCooldown = true;
        attackCooldownTimer = 1f; // Фиксированный кулдаун в 1 секунду
    }

    private void ChooseNextState()
    {
        int r = Random.Range(0, 3);
        switch (r)
        {
            case 0: skeleton.ChangeState(new RetreatState(skeleton)); break;
            case 1: skeleton.ChangeState(new KeepDistanceState(skeleton)); break;
            case 2: skeleton.ChangeState(new ApproachState(skeleton)); break;
        }
    }

    public void Exit()
    {
        skeleton.AIPath.canMove = true;
    }

    // Метод для внешнего прерывания состояния (например, при получении урона)
    public void Interrupt()
    {
        isInterrupted = true;
    }
}
public class TakeDamageState : IState
{
    private SkeletonAI skeleton;
    private float staggerDuration = 1f; 
    private float staggerTimer;
    private bool stun;

    public TakeDamageState(SkeletonAI skeleton)
    {
        this.skeleton = skeleton;
        stun = Random.value > 0.5f;
        if(stun)
            Debug.Log("SKELETON STUN");
    }

    public void Enter()
    {
        skeleton.animator.SetTrigger($"Hit");
        skeleton.AIPath.canMove = false;
        staggerTimer = staggerDuration;
    }

    public void Execute()
    {
        staggerTimer -= Time.deltaTime;
        if (staggerTimer <= 0 || !stun)
        {
            skeleton.ChangeState(new ApproachState(skeleton));
        }
    }

    public void Exit()
    {
        skeleton.AIPath.canMove = true;
    }
}

public class DeadState : IState
{
    private SkeletonAI skeleton;

    public DeadState(SkeletonAI skeleton)
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