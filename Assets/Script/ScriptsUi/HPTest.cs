// 테스트 전용
using UnityEngine;

public class HPTest : MonoBehaviour
{
    [SerializeField]
    private PlayerHUDController playerHUD;

    [SerializeField]
    private BossHUDController bossHUD;

    [SerializeField]
    private CharacterState characterState;

    [SerializeField]
    private Boss boss;

    [SerializeField]
    private int playerDamageAmount = 20;

    [SerializeField]
    private int bossDamageAmount = 100;

    [SerializeField]
    private int healAmount = 30;

    private void Start()
    {
        if (characterState == null)
            Debug.LogWarning("Character연결X");

        if (boss == null || boss.data == null)
            Debug.LogWarning("Boss연결X");
    }

    private void Update()
    {
        // Q: 플레이어 피격
        if (Input.GetKeyDown(KeyCode.Q) && characterState != null)
        {
            DamageVO dmg = new DamageVO
            {
                amount = playerDamageAmount,
                damageType = DamageVO.DamageType.normal,
            };
            characterState.GetDamage(dmg);
        }

        // E: 플레이어 회복
        if (Input.GetKeyDown(KeyCode.E) && characterState != null)
        {
            characterState.CurrentHealth += healAmount;
            // Damaged 이벤트는 피격에만 발생하므로 직접 HUD 갱신
            playerHUD?.OnHealForTest(characterState.CurrentHealth, characterState.maxHealth);
        }

        // W: 보스 피격
        if (Input.GetKeyDown(KeyCode.W) && boss != null)
        {
            DamageVO dmg = new DamageVO
            {
                amount = bossDamageAmount,
                damageType = DamageVO.DamageType.normal,
            };
            boss.GetDamage(dmg);
        }

        // R: 스태미나 소모 테스트
        if (Input.GetKeyDown(KeyCode.R) && characterState != null)
        {
            characterState.CurrentStamina -= 30f;
        }
    }
}
