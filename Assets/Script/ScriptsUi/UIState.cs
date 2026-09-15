using UnityEngine;
using UnityEngine.InputSystem;

public class UIState : MonoBehaviour
{
    [SerializeField]
    private GameObject healOverLay;

    [SerializeField]
    private GameObject dodgeOverLay;

    [SerializeField]
    private CharacterState characterState;

    [SerializeField]
    private float healOverlayTime = 1.0f;

    [SerializeField]
    private float dodgeOverlayTime = 0.5f;

    private InputAction dodge;
    private InputAction useItem;

    private float healTimer;
    private float dodgeTimer;

    private void Start()
    {
        dodge = InputSystem.actions.FindAction("Dodge", true);
        useItem = InputSystem.actions.FindAction("UseItem", true);

        dodge.performed += OnDodge;
        useItem.performed += OnUseItem;

        healOverLay.SetActive(false);
        dodgeOverLay.SetActive(false);
    }

    private void OnDisable()
    {
        if (dodge != null)
        {
            dodge.performed -= OnDodge;
        }

        if (useItem != null)
        {
            useItem.performed -= OnUseItem;
        }
    }

    private void Update()
    {
        if (dodgeTimer > 0f)
        {
            dodgeTimer -= Time.unscaledDeltaTime;
        }

        if (healTimer > 0f)
        {
            healTimer -= Time.unscaledDeltaTime;
        }

        bool isActionDisabled = IsActionDisabled();

        dodgeOverLay.SetActive(isActionDisabled || dodgeTimer > 0f);
        healOverLay.SetActive(isActionDisabled || healTimer > 0f);
    }

    private bool IsActionDisabled()
    {
        if (characterState == null)
            return false;

        return characterState.currentState == CharacterState.StateType.Attack
            || characterState.currentState == CharacterState.StateType.Dodge
            || characterState.currentState == CharacterState.StateType.UsingConsumable
            || characterState.currentState == CharacterState.StateType.Damaged
            || characterState.currentState == CharacterState.StateType.Die;
    }

    private void OnDodge(InputAction.CallbackContext context)
    {
        dodgeTimer = dodgeOverlayTime;
    }

    private void OnUseItem(InputAction.CallbackContext context)
    {
        healTimer = healOverlayTime;
    }
}
