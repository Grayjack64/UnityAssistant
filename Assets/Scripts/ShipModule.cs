using UnityEngine;
using System.Collections.Generic;

public enum ShipModuleSize { Frigate, Cruiser, Battleship }

public enum HardpointType { Wing, Tail, Weapon, Device, Engine, Bow }

[System.Serializable]
public struct Hardpoint
{
    public HardpointType type;
    public Transform transform;
}

[CreateAssetMenu(fileName = "ShipModule", menuName = "Ship Modules/New Ship Module")]
public class ShipModule : ScriptableObject
{
    public string moduleName;
    public ShipModuleSize size;
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