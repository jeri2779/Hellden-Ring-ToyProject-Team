using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterState : MonoBehaviour, IDamageable
{
    public enum StateType
    {
        Idle,
        Move,
        Attack,
        UsingConsumable,
        Dodge,
        Damaged,
        Die,
    }

    public enum StaminaUseType
    {
        NormalAttack = 0, //공격
        RestoreStamina, //초당 회복
        Dodge, //회피
    }

    public enum ConsumableItem
    {
        Heal,
    }

    private CharacterMove characterMove;
    public CharacterAttackZone AttackZone;
    private GameObject boss;
    public int maxHealth = 100;
    private int currentHealth;

    private int consumablesCount; //현재 소모품 갯수
    private ConsumableItem currentConsumable; //현재 소모품 타입(임시 int)

    public int StartPower = 5;
    public int StartHealthStat = 5;
    public int StartDexterity = 5;

    private int power; //힘
    private int healthStat; //체력스텟
    private int dexterity; //민첩

    public float maxStamina; //최대 스테미나
    private float currentStamina; //현재 스테미나
    public float[] stmUseSpeed = new float[] { 20, 0.5f, 4 }; //스테미나 소모 속도[공격(1회)/달리기(초당)/회피(1회)] /임시 값
    public float restoreStmTime = 5f; //스테미나 회복 대기시간
    private float restoreStmTimer = 0f; //대기 타이머
    public float DodgeCooltime = 2f; //닷지 쿨타임
    private float lastDodgeTime = -999f;
    public float HealCoolTime = 8f;
    private float lastHealTime = -999f;

    public StateType currentState = StateType.Idle;

    public bool IsDead => currentHealth <= 0; //사망 여부
    public bool IsDrained => currentStamina <= 0.5f; //스테미나 다떨어진 상태
    public bool CanRestoreStm => restoreStmTimer <= 0f;

    private float lastSentDodgeCooldown = -1f;
    private float lastSentHealCooldown = -1f;

    public event System.Action<int> Damaged; //데미지 이벤트 함수
    public event System.Action<float> OnStaminaChanged; //스테미나 이벤트 함수
    public event System.Action<float> OnDodgeCooldownChanged; //닷지 쿨타임 이벤트 함수
    public event System.Action<float> OnHealCooldownChanged; //닷지 쿨타임 이벤트 함수

    private Animator anim;
    private readonly int Normal = Animator.StringToHash("Normal");
    private readonly int Hard = Animator.StringToHash("Hard");
    private readonly int VeryHard = Animator.StringToHash("VeryHard");
    private readonly int Die = Animator.StringToHash("Dead");
    private readonly string BossTag = "Boss";
    public int attackCount = 0;
    private Coroutine vibration = null;
    private bool canDamage = true;

    [SerializeField]
    private float stmRecoverTime = 1f;
    public bool canDodge { get; private set; } = true;

    public event Action OnGameOver;

    public int CurrentHealth
    {
        get { return currentHealth; }
        set
        {
            float prev = currentHealth;
            currentHealth = Mathf.Clamp(value, 0, maxHealth);

            if (currentHealth <= 0f && currentState != StateType.Die)
            {
                currentState = StateType.Die;
                Dead();
            }

            if (prev != currentHealth)
            {
                if (vibration != null)
                {
                    StopCoroutine(vibration);
                    if (Gamepad.current != null)
                        Gamepad.current.SetMotorSpeeds(0f, 0f);
                }

                vibration = StartCoroutine(HitVibration());

                Damaged?.Invoke(currentHealth);
            }
        }
    }

    public float DodgeCooldownRemaining =>
        Mathf.Max(0, (lastDodgeTime + DodgeCooltime) - Time.time);

    public float HealCooldownRemaining => Mathf.Max(0, (lastHealTime + HealCoolTime) - Time.time);

    public IEnumerator HitVibration()
    {
        if (Gamepad.current == null)
        {
            yield break;
        }
        Gamepad.current.SetMotorSpeeds(0.75f, 0.75f);
        yield return new WaitForSeconds(0.2f);
        Gamepad.current.SetMotorSpeeds(0f, 0f);
        vibration = null;
    }

    public float CurrentStamina
    {
        get { return currentStamina; }
        set
        {
            if (value < currentStamina - 0.01f)
            {
                restoreStmTimer = restoreStmTime;
            }

            float prev = currentStamina;

            currentStamina = Mathf.Clamp(value, 0, maxStamina);

            if (prev != currentStamina)
            {
                OnStaminaChanged?.Invoke(currentStamina);
            }
        }
    }

    public int ConsumablesCount
    {
        get { return consumablesCount; }
        set { consumablesCount = value; }
    }

    public ConsumableItem CurrentConsumable
    {
        get { return currentConsumable; }
        set { currentConsumable = value; }
    }

    public int Power => power;
    public int HealthStat => healthStat;
    public int Dexterity => dexterity;

    void Start()
    {
        CurrentHealth = maxHealth;
        CurrentStamina = maxStamina;

        power = StartPower;
        healthStat = StartHealthStat;
        dexterity = StartDexterity;

        characterMove = GetComponent<CharacterMove>();
        boss = GameObject.FindGameObjectWithTag(BossTag);
        if (boss == null)
            Debug.LogError($"[CharacterState] '{BossTag}' 태그를 가진 오브젝트를 찾을 수 없습니다.");
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (currentState == StateType.Die)
        {
            return;
        }

        if (restoreStmTimer > 0)
        {
            restoreStmTimer -= Time.deltaTime;

            if (restoreStmTimer < 0)
                restoreStmTimer = 0;
        }
        if (CanRestoreStm && currentStamina < maxStamina)
        {
            float recoveryPerSecond = maxStamina / stmRecoverTime;
            CurrentStamina += recoveryPerSecond * Time.deltaTime;
        }

        float DodgeRemain = DodgeCooldownRemaining;

        if (!Mathf.Approximately(DodgeRemain, lastSentDodgeCooldown))
        {
            OnDodgeCooldownChanged?.Invoke(DodgeRemain);
            lastSentDodgeCooldown = DodgeRemain;
        }

        float HealRemain = HealCooldownRemaining;

        if (!Mathf.Approximately(HealRemain, lastSentHealCooldown))
        {
            OnHealCooldownChanged?.Invoke(HealRemain);
            lastSentHealCooldown = HealRemain;
        }
    }

    public void Dead()
    {
        currentState = StateType.Die;
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        canDodge = false;
        anim.SetTrigger(Die);
        characterMove.HealParticle.Stop();
        OnGameOver?.Invoke();
    }

    public void Attacking()
    {
        if (
            currentState == StateType.Dodge
            || currentState == StateType.Damaged
            || currentState == StateType.Die
            || !canDamage
        )
            return;

        canDamage = false;
        currentState = StateType.Attack;
        DamageVO damage = new DamageVO();
        damage.amount = Power;
        damage.damageType = DamageVO.DamageType.soft;

        AttackZone.SetDamage(damage);

        CurrentStamina -= stmUseSpeed[(int)StaminaUseType.NormalAttack];
    }

    public void HardAttacking()
    {
        if (
            currentState == StateType.Dodge
            || currentState == StateType.Damaged
            || currentState == StateType.Die
        )
            return;

        currentState = StateType.Attack;

        DamageVO damage = new DamageVO();
        damage.amount = (int)((float)Power * 1.5);
        damage.damageType = DamageVO.DamageType.soft;

        AttackZone.SetDamage(damage);

        CurrentStamina -= (int)(stmUseSpeed[(int)StaminaUseType.NormalAttack] * 1.5);
    }

    public void Dodging()
    {
        if (
            currentState == StateType.Attack
            || currentState == StateType.Damaged
            || currentState == StateType.Die
        )
            return;

        if (!canDodge)
            return;

        currentState = StateType.Dodge;
        canDodge = false;

        CurrentStamina -= stmUseSpeed[(int)StaminaUseType.Dodge];

        lastDodgeTime = Time.time;

        characterMove.commandQueue.Clear();
    }

    public void CanDodge()
    {
        canDodge = true;
    }

    public float GetDodgeCooldownRatio() //UI용
    {
        return DodgeCooldownRemaining / DodgeCooltime;
    }

    public void UsingConsumable()
    {
        if (
            currentState == StateType.Attack
            || currentState == StateType.Damaged
            || currentState == StateType.Die
        )
            return;

        if (!CanUseHeal())
            return;

        currentState = StateType.UsingConsumable;

        CurrentHealth += 20;

        lastHealTime = Time.time;
    }

    public bool CanUseHeal()
    {
        return Time.time >= lastHealTime + HealCoolTime;
    }

    public void StopHealParticle()
    {
        characterMove.HealParticle.Stop();
    }

    public bool IsInvincible()
    {
        return currentState == StateType.Dodge || currentState == StateType.Die;
    }

    public void GetDamage(DamageVO damageData)
    {
        if (IsInvincible())
            return;

        CurrentHealth -= damageData.amount;

        if (CurrentHealth <= 0)
        {
            return;
        }

        switch (damageData.damageType)
        {
            case DamageVO.DamageType.noDamage:
            case DamageVO.DamageType.soft:
                break;
            case DamageVO.DamageType.normal:
                anim.SetTrigger(Normal);
                canDodge = false;
                characterMove.HealParticle.Stop();
                currentState = StateType.Damaged;
                break;
            case DamageVO.DamageType.hard:
                anim.SetTrigger(Hard);
                currentState = StateType.Damaged;
                characterMove.HealParticle.Stop();
                canDodge = false;
                break;
            case DamageVO.DamageType.veryHard:
                anim.SetTrigger(VeryHard);
                currentState = StateType.Damaged;
                characterMove.HealParticle.Stop();
                canDodge = false;
                break;
            case DamageVO.DamageType.instantKill:
                Dead();
                break;
        }
    }

    public void EnableAttack()
    {
        characterMove.commandQueue.Clear();
        AttackZone.Attackable = true;
        canDamage = true;
        attackCount++;
    }

    public void DisableAttack()
    {
        if (characterMove.commandQueue.Count == 0)
        {
            characterMove.EndAttack();
            AttackZone.Attackable = false;
        }
    }
}
