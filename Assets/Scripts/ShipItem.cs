using UnityEngine;

[CreateAssetMenu(fileName = "ShipItem", menuName = "Ship/Item", order = 2)]
public class ShipItem : ScriptableObject
{
    public string itemName;
    public HardpointType hardpointType;

    // Item stats
    // ...
}