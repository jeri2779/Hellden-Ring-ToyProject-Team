using UnityEngine;

public interface IDamageable
{
    void GetDamage(DamageVO damageData);
}

public struct DamageVO
{
    public enum DamageType
    {
        noDamage,
        soft,
        normal,
        hard,
        veryHard,
        instantKill,
    }

    public int amount;
    public DamageType damageType;
}
