using UnityEngine;
using System.Collections.Generic;

public enum ModuleSize { Frigate, Cruiser, Battleship }

public enum HardpointType { Wing, Tail, Weapon, Device, Engine, Bow, Ammo }

[System.Serializable]
public class Hardpoint
{
    public HardpointType type;
    public ModuleSize size;
}

[CreateAssetMenu(fileName = "ShipModule", menuName = "Ship/Module", order = 1)]
public class ShipModule : ScriptableObject
{
    public string moduleName;
    public ModuleSize size;
    public List<Hardpoint> connectionHardpoints;
    public List<Hardpoint> itemHardpoints;

    public float hull;
    public float repairSpeed;
    public float maxPower;
    public float powerRechargeSpeed;
    public float cpu;
    public float speed;
    public float turnSpeed;
    public float shields;
    public float accuracy;
    public float damage;
    public float money;
}