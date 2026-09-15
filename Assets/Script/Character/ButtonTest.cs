using UnityEngine;
using UnityEngine.UI;

public class ButtonTest : MonoBehaviour
{
    public Button[] buttons;
    public CharacterState characterState;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        buttons[0].onClick.AddListener(() => TestDamage(DamageVO.DamageType.normal));
        buttons[1].onClick.AddListener(() => TestDamage(DamageVO.DamageType.hard));
        buttons[2].onClick.AddListener(() => TestDamage(DamageVO.DamageType.veryHard));
        buttons[3].onClick.AddListener(() => TestDamage(DamageVO.DamageType.instantKill));
    }

    void TestDamage(DamageVO.DamageType type)
    {
        DamageVO dmg = new DamageVO();
        dmg.damageType = type;
        dmg.amount = 10;

        characterState.GetDamage(dmg);
    }
}
