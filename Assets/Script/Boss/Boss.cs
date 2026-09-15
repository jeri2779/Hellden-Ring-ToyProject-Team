using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Boss : MonoBehaviour, IDamageable
{
    private enum Statement
    {
        Idle,
        Attack,
        Death,
        Move,
    }

    private static readonly int MiddleHit = Animator.StringToHash("MiddleHit");
    private static readonly int BigHit = Animator.StringToHash("BigHit");
    private static readonly int Death = Animator.StringToHash("Death");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int BigAttack = Animator.StringToHash("BigAttack");
    private static readonly int Move = Animator.StringToHash("Move");
    private static readonly int Phase2 = Animator.StringToHash("Phase2");
    private static readonly int Forward = Animator.StringToHash("Forward");
    private static readonly int WindMill = Animator.StringToHash("WindMill");
    private static readonly string PlayerTag = "Player";

    private Animator animator;
    public NormalAttackZone attackZone;
    public BossData data;
    private NavMeshAgent agent;
    private GameObject target;
    private CharacterState targetState;
    private Coroutine coroutine;

    private Statement currentstatement;
    private int normalAttackCount = 0;
    private float idleTime = 0f;
    public float idleInterval = 4f;
    public float attackDistance = 0f;
    private float toTargetDistance =>
        target != null ? Vector3.Distance(transform.position, target.transform.position) : float.MaxValue;
    private float attackCoolTime = 0f;
    public float attackInterval = 4f;
    private float forwardAttackCoolTime = 0f; //��������
    public float forwardAttackInterval = 10f;
    private float windMillCoolTime = 0f; //ȸ������
    public float windMillInterval = 10f;
    private bool phase2;
    private int maxHp;
    public int CurrentHp { get; private set; }
    private int damage;
    private bool isDeath;
    private bool invincible;
    private bool isAttack;
    private bool canForward; //�������� ���� ����
    private bool canWindMill;
    private float wait;
    public bool isGameOver { get; private set; } = false;
    private float beforeSpeed;

    public event Action<int> OnDamage;
    public event Action OnClear;

    private Statement CurrentStatement
    {
        get { return currentstatement; }
        set
        {
            switch (value)
            {
                case Statement.Idle:
                    idleTime = 0f;
                    currentstatement = value;
                    agent.isStopped = true;
                    break;
                case Statement.Attack:
                    agent.isStopped = true;
                    currentstatement = value;
                    break;
                case Statement.Death:
                    agent.isStopped = true;
                    currentstatement = value;
                    break;
                case Statement.Move:
                    agent.isStopped = false;
                    currentstatement = value;
                    break;
            }
        }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        attackZone.gameObject.SetActive(false);
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }

        target = GameObject.FindGameObjectWithTag(PlayerTag);

        if (target == null)
        {
            Debug.LogError($"[Boss] '{PlayerTag}' 태그를 가진 오브젝트를 찾을 수 없습니다.");
        }
        else
        {
            targetState = target.GetComponent<CharacterState>();
            if (targetState != null)
            {
                targetState.OnGameOver += OnGameOver;
            }
        }

        CurrentStatement = Statement.Idle;
        phase2 = false;
        maxHp = data.BossHp;
        CurrentHp = maxHp;
        damage = data.Attack;
        isDeath = false;
        invincible = false;
        isAttack = false;
        canWindMill = false;
        wait = 0f;
    }

    private void Update()
    {
        if (target == null || isDeath || isGameOver)
        {
            return;
        }

        agent.SetDestination(target.transform.position);

        if (!phase2 && CurrentHp <= maxHp / 2)
        {
            phase2 = true;
            damage = Mathf.CeilToInt(data.Attack * 1.3f);
            attackInterval = Mathf.Floor(attackInterval * 0.9f);
            idleInterval = Mathf.Floor(idleInterval * 0.9f);
            agent.speed = Mathf.Ceil(agent.speed * 1.1f);
            beforeSpeed = agent.speed;
            agent.isStopped = true;
            animator.SetTrigger(Phase2);
            return;
        }

        attackCoolTime += Time.deltaTime;
        forwardAttackCoolTime += Time.deltaTime;
        if (phase2)
        {
            windMillCoolTime += Time.deltaTime;
        }

        switch (CurrentStatement)
        {
            case Statement.Idle:
                idleTime += Time.deltaTime;
                if (
                    toTargetDistance > attackDistance
                    && toTargetDistance < 7f
                    && forwardAttackCoolTime > forwardAttackInterval
                )
                {
                    canForward = true;
                    CurrentStatement = Statement.Attack;
                }
                else if (phase2 && windMillCoolTime > windMillInterval)
                {
                    canWindMill = true;
                    CurrentStatement = Statement.Attack;
                }
                else if (toTargetDistance < attackDistance)
                {
                    CurrentStatement = Statement.Attack;
                }
                else if (idleTime > idleInterval)
                {
                    CurrentStatement = Statement.Move;
                }
                break;
            case Statement.Attack:
                if (!isAttack && attackCoolTime > attackInterval)
                {
                    OnAttack();
                }
                break;
            case Statement.Death:
                OnDeath();
                break;
            case Statement.Move:
                OnMove();
                if (
                    toTargetDistance > attackDistance
                    && toTargetDistance < 4f
                    && forwardAttackCoolTime > forwardAttackInterval
                )
                {
                    canForward = true;
                    CurrentStatement = Statement.Attack;
                }
                else if (phase2 && windMillCoolTime > windMillInterval)
                {
                    canWindMill = true;
                    CurrentStatement = Statement.Attack;
                }
                else if (toTargetDistance < attackDistance)
                {
                    CurrentStatement = Statement.Attack;
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        if (target == null || isDeath)
        {
            return;
        }

        transform.LookAt(target.transform.position);
    }

    public void OnMiddleHit()
    {
        animator.SetTrigger(MiddleHit);
    }

    public void OnBigHit()
    {
        animator.SetTrigger(BigHit);
    }

    //����׿�
    public void OnPhase2()
    {
        CurrentHp -= CurrentHp / 2;
    }

    public void OnDeath()
    {
        if (isDeath)
        {
            return;
        }

        isDeath = true;
        agent.isStopped = true;
        animator.SetTrigger(Death);
        OnClear?.Invoke();
    }

    public void OnAttack()
    {
        isAttack = true;

        if (canForward)
        {
            animator.SetTrigger(Forward);
            return;
        }
        else if (canWindMill)
        {
            agent.isStopped = false;
            agent.speed *= 1.5f;
            animator.SetTrigger(WindMill);
            return;
        }

        if (toTargetDistance > attackDistance)
        {
            CurrentStatement = Statement.Move;
            isAttack = false;
            return;
        }

        if (normalAttackCount >= 2)
        {
            animator.SetTrigger(BigAttack);
            normalAttackCount = 0;
        }
        else
        {
            animator.SetTrigger(Attack);
            normalAttackCount++;
        }
    }

    public void OnBigAttack()
    {
        animator.SetTrigger(BigAttack);
    }

    public void OnMove()
    {
        animator.SetBool(Move, true);
    }

    public void ToggleAttackZone()
    {
        if (!attackZone.gameObject.activeSelf)
        {
            DamageVO attackInfo = new();

            if (normalAttackCount > 0)
            {
                attackInfo.amount = damage;
                attackInfo.damageType = DamageVO.DamageType.normal;
            }
            else if (canForward)
            {
                attackInfo.amount = damage * 2;
                attackInfo.damageType = DamageVO.DamageType.hard;
            }
            else if (canWindMill)
            {
                attackInfo.amount = damage;
                attackInfo.damageType = DamageVO.DamageType.normal;
            }
            else if (normalAttackCount == 0)
            {
                attackInfo.amount = damage * 4;
                attackInfo.damageType = DamageVO.DamageType.veryHard;
            }

            attackZone.SetDamage(attackInfo);
            attackZone.attackable = true;
            attackZone.gameObject.SetActive(true);
            return;
        }
        attackZone.gameObject.SetActive(false);
    }

    public void AttackAnimationEnd()
    {
        CurrentStatement = Statement.Idle;
        animator.SetBool(Move, false);
        attackCoolTime = 0f;
        isAttack = false;
    }

    public void GetDamage(DamageVO damageData)
    {
        if (invincible)
        {
            return;
        }

        switch (damageData.damageType)
        {
            case DamageVO.DamageType.normal:
                OnMiddleHit();
                break;
            case DamageVO.DamageType.hard:
            case DamageVO.DamageType.veryHard:
                OnBigHit();
                break;
            case DamageVO.DamageType.instantKill:
                OnDeath();
                break;
            case DamageVO.DamageType.noDamage:
            case DamageVO.DamageType.soft:
            default:
                break;
        }

        CurrentHp -= damageData.amount;
        OnDamage?.Invoke(damageData.amount);

        if (CurrentHp <= 0)
        {
            CurrentHp = 0;
            CurrentStatement = Statement.Death;
        }
    }

    public void Phase2MotionEnd()
    {
        if (CurrentStatement == Statement.Attack)
        {
            AttackAnimationEnd();
        }

        agent.isStopped = CurrentStatement == Statement.Move ? false : true;
    }

    public void ToggleInvincible()
    {
        invincible = !invincible;
    }

    public void ForwardEnd()
    {
        forwardAttackCoolTime = 0f;
        canForward = false;
        AttackAnimationEnd();
    }

    public void WindMillEnd()
    {
        windMillCoolTime = 0f;
        canWindMill = false;
        agent.speed = beforeSpeed;
        AttackAnimationEnd();
    }

    public void Waiting()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }

        coroutine = StartCoroutine(CoWaiting());
    }

    public IEnumerator CoWaiting()
    {
        animator.speed = 0.1f;
        while (wait < 1f)
        {
            wait += Time.deltaTime;
            yield return null;
        }
        animator.speed = 1f;
        wait = 0f;
        coroutine = null;
    }

    public void OnGameOver()
    {
        CurrentStatement = Statement.Idle;
        if (targetState != null)
        {
            targetState.OnGameOver -= OnGameOver;
            targetState = null;
        }
        target = null;
        isGameOver = true;
    }

    private void OnDisable()
    {
        if (targetState != null)
        {
            targetState.OnGameOver -= OnGameOver;
            targetState = null;
        }
    }
}
