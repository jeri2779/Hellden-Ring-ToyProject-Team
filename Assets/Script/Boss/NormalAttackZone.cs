using UnityEngine;

public class NormalAttackZone : MonoBehaviour
{
    public Boss Boss;
    private BoxCollider attackZone;
    private IDamageable player;
    public bool attackable;
    private DamageVO damage;

    private void OnEnable()
    {
        attackZone = GetComponent<BoxCollider>();
    }

    public void SetDamage(DamageVO damageInfo)
    {
        damage = damageInfo;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Boss.isGameOver)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            player = other.gameObject.GetComponent<IDamageable>();
            if (player != null && attackable)
            {
                attackable = false;
                player.GetDamage(damage);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.gameObject.GetComponent<IDamageable>();
            if (player != null)
            {
                attackable = true;
            }
        }
    }

    private void OnDisable()
    {
        attackable = false;
    }
}
