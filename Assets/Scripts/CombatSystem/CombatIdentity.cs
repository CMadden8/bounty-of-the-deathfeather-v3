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

        [Header("Status Effects")]
        public List<StatusEffect> Statuses = new List<StatusEffect>();

        public ArmourPools GetArmourPools() => new ArmourPools(ArmourPiercing, ArmourSlashing, ArmourBludgeoning);

        public UnitStats ToUnitStats()
        {
            return new UnitStats(
                lifeHP: LifeHP,
                maxLifeHP: MaxLifeHP,
                armour: GetArmourPools(),
                maxArmour: GetArmourPools(),
                actionPoints: ActionPoints,
                maxActionPoints: ActionPoints,
                movementPoints: MovementPoints,
                maxMovementPoints: MovementPoints,
                statuses: Statuses
            );
        }

        public void AddStatus(StatusEffect status)
        {
            // Simple add for now; real logic might merge stacks
            Statuses.Add(status);
        }
    }
}
