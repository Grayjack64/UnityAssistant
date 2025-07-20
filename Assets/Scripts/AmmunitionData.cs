using UnityEngine;

[CreateAssetMenu(fileName = "AmmunitionData", menuName = "Ammunition Data", order = 52)]
public class AmmunitionData : ScriptableObject
{
    // Define ammunition properties here. Examples:
    public float DamageModifier;
    public float RateOfFireModifier;
    public float AccuracyModifier;
    // ... other modifiers
}