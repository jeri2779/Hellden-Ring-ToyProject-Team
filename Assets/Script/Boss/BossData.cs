using UnityEngine;

[CreateAssetMenu(fileName = "BossData", menuName = "Scriptable Objects/BossData")]
public class BossData : ScriptableObject
{
    public int BossHp = 100;
    public string BossName = "데스나이트";
    public int Attack = 1;
}
