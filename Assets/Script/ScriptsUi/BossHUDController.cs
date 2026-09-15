using TMPro;
using UnityEngine;

public class BossHUDController : MonoBehaviour
{
    [SerializeField]
    private DualSliderBar hpBar;

    [SerializeField]
    private TMP_Text bossNameText;

    [SerializeField]
    private Boss boss;

    private int startHp;

    private void Start()
    {
        if (boss == null || boss.data == null)
        {
            Debug.LogWarning("보스 데이터 X");
            return;
        }

        startHp = boss.data.BossHp;

        if (bossNameText != null)
        {
            bossNameText.text = boss.data.BossName;
        }

        hpBar.SetValue(startHp, startHp);

        boss.OnDamage += OnBossDamaged;
    }

    private void OnDestroy()
    {
        if (boss != null)
            boss.OnDamage -= OnBossDamaged;
    }

    private void OnBossDamaged(int dmg)
    {
        hpBar.SetValue(boss.CurrentHp, startHp);
    }
}
