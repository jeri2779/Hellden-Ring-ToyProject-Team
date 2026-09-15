using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static CharacterState;

public class CharacterMove : MonoBehaviour
{
    private CharacterState state;
    private Animator anim;
    private Rigidbody rb;
    private GameObject boss;

    public ParticleSystem HealParticle;
    public GameObject startMenu;
    public GameObject optionMenu;
    public GameObject pauseMenu;
    public GameObject DodgeView;
    public GameObject LockOnPoint;

    public float normalSpeed = 5; //평상시 속력
    public float dodgeSpeed = 8f; //드레인 상태시 속력
    public float rotateSpeed = 0.3f;
    public float drainedSpeedMultiplier = 0.3f; // 느려지는 정도
    private float speed; //적용할 속도

    private Vector3 dodgeDirection; //닷지 시 방향 저장

    public static readonly int Attack = Animator.StringToHash("Attack");
    public static readonly int Dodge = Animator.StringToHash("Dodge");
    public static readonly int HardAttack = Animator.StringToHash("HardAttack");
    public static readonly int Heal = Animator.StringToHash("Heal");

    public List<string> commandQueue = new();
    private bool canCommand = false;

    [SerializeField]
    private int maxCommand = 2;
    private bool lockOn = false;

    Vector2 moveValue = Vector2.zero;

    //InputActions
    InputAction move;
    InputAction look;
    InputAction attack;
    InputAction dodge;
    InputAction hardAttack;
    InputAction useItem;
    InputAction changeItem;
    InputAction LockOn;

    void Start()
    {
        state = GetComponent<CharacterState>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        HealParticle.Stop();
        boss = GameObject.FindWithTag("Boss");
        if (boss == null)
            Debug.LogError("[CharacterMove] 'Boss' 태그를 가진 오브젝트를 찾을 수 없습니다.");

        move = InputSystem.actions.FindAction("Move", true);
        look = InputSystem.actions.FindAction("Look", true);
        attack = InputSystem.actions.FindAction("Attack", true);
        dodge = InputSystem.actions.FindAction("Dodge", true);
        hardAttack = InputSystem.actions.FindAction("StrongAttack", true);
        useItem = InputSystem.actions.FindAction("UseItem", true);
        changeItem = InputSystem.actions.FindAction("ChangeItem", true);
        LockOn = InputSystem.actions.FindAction("LockOn", true);

        attack.performed += OnAttackKey;
        dodge.performed += OnDodge;
        useItem.performed += OnUseConsumable;
        LockOn.performed += BossLockOn;

        if (Gamepad.current != null)
            Gamepad.current.SetMotorSpeeds(0f, 0f);
    }

    private void OnDisable()
    {
        attack.performed -= OnAttackKey;
        dodge.performed -= OnDodge;
        useItem.performed -= OnUseConsumable;
        LockOn.performed -= BossLockOn;
        if (Gamepad.current != null)
            Gamepad.current.SetMotorSpeeds(0f, 0f);
    }

    private void Update()
    {
        if (state.currentState == StateType.Die)
        {
            return;
        }

        speed = normalSpeed;
        if (state.IsDrained)
        {
            speed *= drainedSpeedMultiplier;
        }

        moveValue = move.ReadValue<Vector2>();

        if (!lockOn)
            transform.Rotate(
                0f,
                look.ReadValue<Vector2>().x * rotateSpeed * Time.timeScale,
                0f,
                Space.World
            );

        if (
            state.currentState != StateType.Attack
            && state.currentState != StateType.Dodge
            && state.currentState != StateType.UsingConsumable
            && state.currentState != StateType.Damaged
        )
        {
            bool isMoving = moveValue.magnitude > 0.001f;

            state.currentState = isMoving ? StateType.Move : StateType.Idle;
        }

        if (state.currentState == StateType.Move)
        {
            anim.SetBool("IsMoving", true);
            anim.SetFloat("MoveX", moveValue.x);
            anim.SetFloat("MoveZ", moveValue.y);
        }
        else
        {
            anim.SetBool("IsMoving", false);
        }
    }

    private void OnAttackKey(InputAction.CallbackContext context)
    {
        if (hardAttack.IsPressed())
        {
            OnHardAttack();
        }
        else
        {
            if (canCommand && state.attackCount > 0)
            {
                commandQueue.Add("A");
                if (commandQueue.Count > maxCommand)
                {
                    commandQueue.RemoveAt(0);
                }
            }
            else
            {
                OnAttack();
            }
        }
    }

    private void BossLockOn(InputAction.CallbackContext context)
    {
        if (boss == null) return;
        lockOn = !lockOn;
        LockOnPoint.SetActive(lockOn);
    }

    private void FixedUpdate()
    {
        if (state.currentState == StateType.Die)
        {
            return;
        }

        if (lockOn)
        {
            if (boss == null || !boss.activeInHierarchy)
            {
                lockOn = false;
                LockOnPoint.SetActive(false);
            }
            else
            {
                transform.LookAt(boss.transform);
                DodgeView.transform.LookAt(boss.transform);
            }
        }

        if (
            state.currentState == CharacterState.StateType.Attack
            || state.currentState == CharacterState.StateType.UsingConsumable
            || state.currentState == CharacterState.StateType.Damaged
        )
        {
            rb.linearVelocity = Vector3.zero;
            DodgeView.transform.position = transform.position;
            DodgeView.transform.rotation = transform.rotation;
            return;
        }
        else if (state.currentState == CharacterState.StateType.Dodge)
        {
            rb.linearVelocity = dodgeDirection * dodgeSpeed;
            transform.rotation = Quaternion.LookRotation(dodgeDirection);
            DodgeView.transform.position = transform.position;
        }
        else
        {
            Vector3 direction = transform.right * moveValue.x + transform.forward * moveValue.y;
            direction = Vector3.ClampMagnitude(direction, 1f);
            rb.linearVelocity = direction * speed;
            DodgeView.transform.position = transform.position;
            DodgeView.transform.rotation = transform.rotation;
        }
    }

    private void OnAttack()
    {
        if (
            state.currentState != StateType.Idle
            && state.currentState != StateType.Move
            && state.currentState != StateType.Attack
        )
            return;

        if (state.CurrentStamina <= 0)
        {
            EndAttack();
            return;
        }

        state.currentState = CharacterState.StateType.Attack;

        state.Attacking();
        anim.SetTrigger(Attack);
    }

    private void OnHardAttack()
    {
        if (state.currentState != StateType.Idle && state.currentState != StateType.Move)
            return;

        if (state.CurrentStamina <= 0)
        {
            EndAttack();
            return;
        }

        state.HardAttacking();
        anim.SetTrigger(HardAttack);
    }

    private void OnUseConsumable(InputAction.CallbackContext context)
    {
        if (state.currentState != StateType.Idle && state.currentState != StateType.Move)
            return;

        if (!state.CanUseHeal())
            return;

        state.UsingConsumable();
        anim.SetTrigger(Heal);
        HealParticle.Play();
    }

    private void OnDodge(InputAction.CallbackContext context)
    {
        if (state.currentState != StateType.Idle && state.currentState != StateType.Move)
            return;
        dodgeDirection = transform.right * moveValue.x + transform.forward * moveValue.y;

        if (dodgeDirection == Vector3.zero)
            dodgeDirection = transform.forward;

        dodgeDirection.Normalize();

        if (!state.canDodge || state.CurrentStamina <= 0f)
            return;

        state.Dodging();
        anim.SetTrigger(Dodge);
    }

    public void EndAttack()
    {
        state.attackCount = 0;
        anim.ResetTrigger(Attack);
        if (state.currentState == CharacterState.StateType.Die)
        {
            return;
        }
        state.currentState = CharacterState.StateType.Idle;
    }

    public void OpenQueue()
    {
        canCommand = true;
    }

    public void CloseQueue()
    {
        canCommand = false;
        TryNextAttack();
    }

    private void TryNextAttack()
    {
        if (commandQueue.Count == 0 || state.CurrentStamina <= 0)
        {
            EndAttack();
            return;
        }

        if (commandQueue.Last() == "A")
        {
            anim.SetTrigger(Attack);
            state.Attacking();
        }
    }

    public void DodgeEnd()
    {
        transform.rotation = DodgeView.transform.rotation;
        state.CanDodge();
    }
}
