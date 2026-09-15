using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDController : MonoBehaviour
{
    [SerializeField]
    private DualSliderBar hpBar;

    [SerializeField]
    private StaminaBar staminaBar;

    [SerializeField]
    private CharacterState characterState;

    private bool initialized = false;

    private void Start()
    {
        if (characterState == null || !characterState.gameObject.activeInHierarchy)
            characterState = FindAnyObjectByType<CharacterState>();

        if (characterState == null)
        {
            Debug.LogWarning("CharacterState 없음");
            return;
        }

        characterState.Damaged += OnDamaged;
        characterState.OnStaminaChanged += OnStaminaChanged;
    }

    private void Update()
    {
        if (initialized || characterState == null)
            return;
        if (characterState.CurrentHealth <= 0)
            return;

        initialized = true;
        hpBar.SetValue(characterState.CurrentHealth, characterState.maxHealth);
        staminaBar.SetValue(characterState.CurrentStamina, characterState.maxStamina);
    }

    private void OnDestroy()
    {
        if (characterState == null)
            return;
        characterState.Damaged -= OnDamaged;
        characterState.OnStaminaChanged -= OnStaminaChanged;
    }

    private void OnDamaged(int dmg)
    {
        if (characterState == null)
            return;
        hpBar.SetValue(characterState.CurrentHealth, characterState.maxHealth);
    }

    private void OnStaminaChanged(float current)
    {
        if (characterState == null)
            return;
        staminaBar.SetValue(current, characterState.maxStamina);
    }

    public void OnHealForTest(int current, int max)
    {
        hpBar.SetValue(current, max);
    }
}
