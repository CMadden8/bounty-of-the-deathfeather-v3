using System;
using System.Collections.Generic;

namespace BountyOfTheDeathfeather.CombatSystem
{
    /// <summary>
    /// Core stat container for a unit. Immutable by design; modifications return new instances.
    /// Represents the deterministic state per COMBAT_MECHANICS.md.
    /// </summary>
    [Serializable]
    public class UnitStats
    {
        public readonly int LifeHP;
        public readonly int MaxLifeHP;
        public readonly ArmourPools Armour;
        public readonly ArmourPools MaxArmour;
        public readonly int ActionPoints;
        public readonly int MaxActionPoints;
        public readonly int MovementPoints;
        public readonly int MaxMovementPoints;
        public readonly int Experience;
        public readonly int Level;
        public readonly int RespecPoints;

        /// <summary>
        /// List of active status effects (e.g., Burning, Poisoned).
        /// </summary>
        public readonly IReadOnlyList<StatusEffect> Statuses;

        /// <summary>
        /// List of equipped abilities.
        /// </summary>
        public readonly IReadOnlyList<string> AbilityIds; // or IAbility references

        /// <summary>
        /// Gear items with their remaining uses.
        /// </summary>
        public readonly IReadOnlyDictionary<string, int> GearUses; // gearId -> remaining uses

        public UnitStats(
            int lifeHP,
            int maxLifeHP,
            ArmourPools armour,
            ArmourPools maxArmour,
            int actionPoints,
            int maxActionPoints,
            int movementPoints,
            int maxMovementPoints,
            int experience = 0,
            int level = 1,
            int respecPoints = 0,
            IReadOnlyList<StatusEffect> statuses = null,
            IReadOnlyList<string> abilityIds = null,
            IReadOnlyDictionary<string, int> gearUses = null)
        {
            LifeHP = Math.Max(0, lifeHP);
            MaxLifeHP = Math.Max(1, maxLifeHP);
            Armour = armour;
            MaxArmour = maxArmour;
            ActionPoints = Math.Max(0, actionPoints);
            MaxActionPoints = Math.Max(0, maxActionPoints);
            MovementPoints = Math.Max(0, movementPoints);
            MaxMovementPoints = Math.Max(0, maxMovementPoints);
            Experience = Math.Max(0, experience);
            Level = Math.Max(1, level);
            RespecPoints = Math.Max(0, respecPoints);
            Statuses = statuses ?? new List<StatusEffect>();
            AbilityIds = abilityIds ?? new List<string>();
            GearUses = gearUses ?? new Dictionary<string, int>();
        }

        /// <summary>
        /// Returns true if the unit is alive (LifeHP > 0).
        /// </summary>
        public bool IsAlive => LifeHP > 0;

        /// <summary>
        /// Returns true if the unit can perform an action (ActionPoints > 0 and alive).
        /// </summary>
        public bool CanAct => IsAlive && ActionPoints > 0;

        /// <summary>
        /// Returns true if the unit can move (MovementPoints > 0 and alive).
        /// </summary>
        public bool CanMove => IsAlive && MovementPoints > 0;

        /// <summary>
        /// Creates a new UnitStats with Life HP modified.
        /// </summary>
        public UnitStats WithLifeHP(int newLifeHP) =>
            new UnitStats(newLifeHP, MaxLifeHP, Armour, MaxArmour, ActionPoints, MaxActionPoints,
                MovementPoints, MaxMovementPoints, Experience, Level, RespecPoints, Statuses, AbilityIds, GearUses);

        /// <summary>
        /// Creates a new UnitStats with Armour modified.
        /// </summary>
        public UnitStats WithArmour(ArmourPools newArmour) =>
            new UnitStats(LifeHP, MaxLifeHP, newArmour, MaxArmour, ActionPoints, MaxActionPoints,
                MovementPoints, MaxMovementPoints, Experience, Level, RespecPoints, Statuses, AbilityIds, GearUses);

        /// <summary>
        /// Creates a new UnitStats with ActionPoints modified.
        /// </summary>
        public UnitStats WithActionPoints(int newActionPoints) =>
            new UnitStats(LifeHP, MaxLifeHP, Armour, MaxArmour, newActionPoints, MaxActionPoints,
                MovementPoints, MaxMovementPoints, Experience, Level, RespecPoints, Statuses, AbilityIds, GearUses);

        /// <summary>
        /// Creates a new UnitStats with MovementPoints modified.
        /// </summary>
        public UnitStats WithMovementPoints(int newMovementPoints) =>
            new UnitStats(LifeHP, MaxLifeHP, Armour, MaxArmour, ActionPoints, MaxActionPoints,
                newMovementPoints, MaxMovementPoints, Experience, Level, RespecPoints, Statuses, AbilityIds, GearUses);

        /// <summary>
        /// Creates a new UnitStats with updated status list.
        /// </summary>
        public UnitStats WithStatuses(IReadOnlyList<StatusEffect> newStatuses) =>
            new UnitStats(LifeHP, MaxLifeHP, Armour, MaxArmour, ActionPoints, MaxActionPoints,
                MovementPoints, MaxMovementPoints, Experience, Level, RespecPoints, newStatuses, AbilityIds, GearUses);

        /// <summary>
        /// Creates a new UnitStats with gear uses updated.
        /// </summary>
        public UnitStats WithGearUses(IReadOnlyDictionary<string, int> newGearUses) =>
            new UnitStats(LifeHP, MaxLifeHP, Armour, MaxArmour, ActionPoints, MaxActionPoints,
                MovementPoints, MaxMovementPoints, Experience, Level, RespecPoints, Statuses, AbilityIds, newGearUses);

        /// <summary>
        /// Resets ActionPoints and MovementPoints to max. Called at turn start per COMBAT_MECHANICS.md.
        /// </summary>
        public UnitStats ResetForTurnStart() =>
            new UnitStats(LifeHP, MaxLifeHP, Armour, MaxArmour, MaxActionPoints, MaxActionPoints,
                MaxMovementPoints, MaxMovementPoints, Experience, Level, RespecPoints, Statuses, AbilityIds, GearUses);

        public override string ToString() =>
            $"UnitStats(Life:{LifeHP}/{MaxLifeHP}, {Armour}, AP:{ActionPoints}/{MaxActionPoints}, MP:{MovementPoints}/{MaxMovementPoints}, Lvl:{Level}, XP:{Experience})";
    }
}
