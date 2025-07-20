using UnityEngine;

[CreateAssetMenu(fileName = "ShipItem", menuName = "Ship/Item", order = 1)]
public class ShipItem : ScriptableObject
{
    public string itemName;
    public HardpointType requiredHardpoint;

    // Item Stats
    // ... (Similar to ShipModule stats)
}