using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Data/Weapon")]
public class WeaponData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public float attackPower;
}
