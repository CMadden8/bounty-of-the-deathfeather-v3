using System;
using System.Collections.Generic;
using UnityEngine;

namespace BountyOfTheDeathfeather.CombatSystem
{
    /// <summary>
    /// Attach to Unity `Unit` GameObjects to hold authoritative combat stats used by the CombatSystem.
    /// This bridges the Unity `Unit` inspector fields and the pure C# `UnitStats` model.
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatIdentity : MonoBehaviour
    {
        [Header("Life")]
        public int LifeHP = 20;
        public int MaxLifeHP = 20;

        [Header("Armour Pools")]
        public int ArmourPiercing = 0;
        public int ArmourSlashing = 0;
        public int ArmourBludgeoning = 0;

        [Header("Combat")]
        public DamageType PrimaryDamageType = DamageType.Piercing;
        public int AttackPower = 1;
        public int AttackRange = 1;

        [Header("Action/Movement")]
        public int ActionPoints = 2;
        public int MovementPoints = 5;

        [Header("Talent Points (TP)")]
        [Tooltip("Per-combat resource for unlocking abilities. Resets at end of battle.")]
        public int TalentPoints = 2;
        public int MaxTalentPoints = 2;
        
        [Header("Talents")]
        [Header("Fire Spirit")]
        [Tooltip("TP invested to unlock Fire Spirit (2 TP to unlock)")]
        public int FireSpiritTPInvested = 0;
        
        [Header("Fire Spirit Supports")]
        [Tooltip("TP invested in Explosion (1 TP per level, increases AOE radius)")]
        public int FireSpiritExplosionTPInvested = 0;
        [Tooltip("TP invested in Wildfire (1 TP per level, adds Panicked chance)")]
        public int FireSpiritWildfireTPInvested = 0;
        [Tooltip("TP invested in Furnace (1 TP per level, adds extra damage chance)")]
        public int FireSpiritFurnaceTPInvested = 0;

        [Header("Gear Points (GP)")]
        [Tooltip("Per-turn resource for using gear. Resets at start of each turn.")]
        public int GearPoints = 1;
        public int MaxGearPoints = 1;

        [Header("Gear")]
        [Tooltip("Gear items with remaining uses (per-battle). Format: gear name, remaining uses.")]
        public List<GearItem> Gear = new List<GearItem>();

        [Header("Status Effects")]
        public List<StatusEffect> Statuses = new List<StatusEffect>();

        public ArmourPools GetArmourPools() => new ArmourPools(ArmourPiercing, ArmourSlashing, ArmourBludgeoning);

        public UnitStats ToUnitStats()
        {
            var gearDict = new Dictionary<string, int>();
            foreach (var item in Gear)
            {
                if (!string.IsNullOrEmpty(item.Name))
                {
                    gearDict[item.Name] = item.RemainingUses;
                }
            }

            return new UnitStats(
                lifeHP: LifeHP,
                maxLifeHP: MaxLifeHP,
                armour: GetArmourPools(),
                maxArmour: GetArmourPools(),
                actionPoints: ActionPoints,
                maxActionPoints: ActionPoints,
                movementPoints: MovementPoints,
                maxMovementPoints: MovementPoints,
                statuses: Statuses,
                gearUses: gearDict
            );
        }

        public void AddStatus(StatusEffect status)
        {
            // Simple add for now; real logic might merge stacks
            Statuses.Add(status);
        }

        public int GetGearUses(string gearName)
        {
            var item = Gear.Find(g => g.Name == gearName);
            return item != null ? item.RemainingUses : 0;
        }

        public bool UseGear(string gearName)
        {
            var item = Gear.Find(g => g.Name == gearName);
            if (item != null && item.RemainingUses > 0)
            {
                item.RemainingUses--;
                return true;
            }
            return false;
        }
    }

    [Serializable]
    public class GearItem
    {
        public string Name;
        public int RemainingUses;
        public int MaxUses;
    }
}
