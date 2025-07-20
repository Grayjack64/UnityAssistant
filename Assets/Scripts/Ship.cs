using UnityEngine;
using System.Collections.Generic;

public class Ship : MonoBehaviour
{
    public List<ShipModule> modules = new List<ShipModule>();

    // Stats calculated from equipped modules
    public float totalHull;
    public float totalRepairSpeed;
    public float totalMaxPower;
    public float totalPowerRechargeSpeed;
    public float totalCPU;
    public float totalSpeed;
    public float totalTurnSpeed;
    public float totalShields;
    public float totalAccuracy;
    public float totalDamage;

    public string shipName;

    public void UpdateStats()
    {
        // Recalculate ship stats based on attached modules.
        totalHull = 0;
        // ... calculate other stats

        foreach (ShipModule module in modules)
        {
            totalHull += module.hull;
            // ... add other stats
        }
    }
}