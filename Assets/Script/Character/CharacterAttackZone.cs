using UnityEngine;

public class CharacterAttackZone : MonoBehaviour
{
    public CharacterState characterState;
    private BoxCollider characterAttackZone;
    private IDamageable boss;
    public bool Attackable;
    private DamageVO damage;

    private void OnEnable()
    {
        characterAttackZone = GetComponent<BoxCollider>();
    }

    public void SetDamage(DamageVO damageInfo)
    {
        damage = damageInfo;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Boss"))
        {
            boss = other.gameObject.GetComponent<IDamageable>();
            if (boss != null && Attackable)
            {
                Attackable = false;
                boss.GetDamage(damage);
            }
        }
    }

    private void OnDisable()
    {
        Attackable = false;
    }
}
