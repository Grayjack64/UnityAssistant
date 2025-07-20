using UnityEngine;
using System;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon Data", order = 51)]
public class WeaponData : ScriptableObject
{
    public enum WeaponCategory { Laser, Plasma, Launcher, Projectile }
    public WeaponCategory Category;

    public float Accuracy;
    public float RateOfFire;

    [Serializable]
    public struct Damage
    {
        public float Hull;
        public float Shields;
    }
    public Damage DamageStats;

    public string AmmunitionType; // Consider making this an enum or ScriptableObject later for better data management

    public float PowerConsumption;
}