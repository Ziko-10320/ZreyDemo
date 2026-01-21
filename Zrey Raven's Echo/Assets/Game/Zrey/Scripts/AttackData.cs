using UnityEngine;

[CreateAssetMenu(fileName = "New AttackData", menuName = "Combat/Attack Data")]
public class AttackData : ScriptableObject
{
    [Header("Attack Properties")]
    public int damage = 10;
    public string hitType = "back";

    [Header("Knockback Properties")]
    public float knockbackDistance = 2f;
    public float knockbackDuration = 0.2f;
}
