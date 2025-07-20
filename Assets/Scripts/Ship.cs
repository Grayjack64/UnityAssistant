using UnityEngine;
using System.Collections.Generic;

public class Ship : MonoBehaviour
{
    public ShipModule body;
    public List<ShipModule> attachedModules = new List<ShipModule>();

    // Calculated stats
    public float totalHull;
    public float totalRepairSpeed;
    // ... other stats

    void UpdateStats()
    {
        // Calculate total stats based on attached modules
    }
}