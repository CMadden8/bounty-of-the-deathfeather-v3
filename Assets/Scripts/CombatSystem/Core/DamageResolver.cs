using System;
using System.Collections.Generic;
using System.Linq;

namespace BountyOfTheDeathfeather.CombatSystem
{
    /// <summary>
    /// Represents a single damage component with type and value.
    /// Multi-type attacks consist of multiple components resolved in order.
    /// </summary>
    public struct DamageComponent
    {
        public DamageType Type;
        public int Value;

        public DamageComponent(DamageType type, int value)
        {
            Type = type;
            Value = Math.Max(0, value);
        }

        public override string ToString() => $"{Type}:{Value}";
    }

    /// <summary>
    /// Result of damage resolution, tracking applied damage and final unit state.
    /// </summary>
    public class DamageResult
    {
        public readonly UnitStats FinalStats;
        public readonly int TotalDamageToArmour;
        public readonly int TotalDamageToLife;
        public readonly bool WasKilled;
        public readonly Dictionary<DamageType, int> DamageByType;

        public DamageResult(
            UnitStats finalStats,
            int totalDamageToArmour,
            int totalDamageToLife,
            bool wasKilled,
            Dictionary<DamageType, int> damageByType)
        {
            FinalStats = finalStats;
            TotalDamageToArmour = totalDamageToArmour;
            TotalDamageToLife = totalDamageToLife;
            WasKilled = wasKilled;
            DamageByType = damageByType ?? new Dictionary<DamageType, int>();
        }
    }

    /// <summary>
    /// Context for damage resolution containing attacker/defender state and modifiers.
    /// </summary>
    public class DamageContext
    {
        public UnitStats Attacker;
        public UnitStats Defender;
        public float AccuracyModifier = 0f; // additive modifier to base accuracy
        public float DamageModifier = 1f; // multiplicative modifier to damage
        public bool BypassArmour = false; // e.g., Poison bypasses armour to Life HP
        public bool IsCritical = false; // determined by accuracy roll

        public DamageContext(UnitStats attacker, UnitStats defender)
        {
            Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
            Defender = defender ?? throw new ArgumentNullException(nameof(defender));
        }
    }

    /// <summary>
    /// Pure service implementing deterministic damage resolution per COMBAT_MECHANICS.md.
    /// Handles multi-component damage, armour spill rules, accuracy rolls, and Life HP bypass.
    /// </summary>
    public class DamageResolver
    {
        private readonly IRandomProvider _random;

        // Default accuracy per COMBAT_MECHANICS.md (adjust based on tier/status)
        private const float BaseAccuracy = 0.85f;
        private const float CriticalThreshold = 0.95f; // rolls >= 95% are critical hits

        public DamageResolver(IRandomProvider random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Resolves an attack with multiple damage components against a defender.
        /// Per COMBAT_MECHANICS.md: single accuracy roll, then resolve components in order (Piercing→Slashing→Bludgeoning).
        /// Returns DamageResult with final defender stats and damage breakdown.
        /// </summary>
        public DamageResult ResolveAttack(
            List<DamageComponent> damageComponents,
            DamageContext context)
        {
            if (damageComponents == null || damageComponents.Count == 0)
                throw new ArgumentException("Damage components cannot be null or empty", nameof(damageComponents));

            // Step 1: Single accuracy roll for the entire attack
            float accuracyRoll = _random.Value();
            float finalAccuracy = BaseAccuracy + context.AccuracyModifier;
            bool hit = accuracyRoll < finalAccuracy;
            context.IsCritical = hit && accuracyRoll >= CriticalThreshold;

            if (!hit)
            {
                // Miss - no damage applied
                return new DamageResult(
                    context.Defender,
                    totalDamageToArmour: 0,
                    totalDamageToLife: 0,
                    wasKilled: false,
                    damageByType: new Dictionary<DamageType, int>());
            }

            // Step 2: Sort components by type priority (Piercing → Slashing → Bludgeoning)
            var orderedComponents = damageComponents
                .OrderBy(c => c.Type)
                .ToList();

            // Step 3: Apply damage modifiers (critical, context modifiers)
            float damageMultiplier = context.DamageModifier;
            if (context.IsCritical)
            {
                damageMultiplier *= 1.5f; // Critical hits deal 50% more damage (adjust per COMBAT_MECHANICS if different)
            }

            // Step 4: Resolve each component in order
            var currentStats = context.Defender;
            int totalArmourDamage = 0;
            int totalLifeDamage = 0;
            var damageByType = new Dictionary<DamageType, int>();

            foreach (var component in orderedComponents)
            {
                int damage = (int)Math.Round(component.Value * damageMultiplier);
                if (damage <= 0) continue;

                damageByType[component.Type] = damageByType.GetValueOrDefault(component.Type, 0) + damage;

                if (context.BypassArmour)
                {
                    // Bypass armour entirely (e.g., Poison) - directly damage Life HP
                    int lifeDamage = Math.Min(damage, currentStats.LifeHP);
                    currentStats = currentStats.WithLifeHP(currentStats.LifeHP - lifeDamage);
                    totalLifeDamage += lifeDamage;
                }
                else
                {
                    // Normal flow: damage armour pool, then spill to Life HP if armour depleted
                    var (newArmour, overflow) = currentStats.Armour.ApplyDamage(component.Type, damage);
                    currentStats = currentStats.WithArmour(newArmour);
                    totalArmourDamage += (damage - overflow);

                    // Check if all armour pools are depleted - if so, overflow goes to Life HP
                    if (overflow > 0 && newArmour.IsFullyDepleted)
                    {
                        int lifeDamage = Math.Min(overflow, currentStats.LifeHP);
                        currentStats = currentStats.WithLifeHP(currentStats.LifeHP - lifeDamage);
                        totalLifeDamage += lifeDamage;
                    }
                }
            }

            bool wasKilled = currentStats.LifeHP <= 0;

            return new DamageResult(
                currentStats,
                totalArmourDamage,
                totalLifeDamage,
                wasKilled,
                damageByType);
        }

        /// <summary>
        /// Convenience method for single-component damage.
        /// </summary>
        public DamageResult ResolveSingleDamage(
            DamageType type,
            int value,
            DamageContext context)
        {
            return ResolveAttack(new List<DamageComponent> { new DamageComponent(type, value) }, context);
        }

        /// <summary>
        /// Applies direct Life HP damage (e.g., Poison tick, falling damage).
        /// Bypasses accuracy and armour entirely.
        /// </summary>
        public DamageResult ApplyDirectLifeDamage(UnitStats target, int damage)
        {
            if (damage <= 0)
            {
                return new DamageResult(target, 0, 0, false, new Dictionary<DamageType, int>());
            }

            int actualDamage = Math.Min(damage, target.LifeHP);
            var newStats = target.WithLifeHP(target.LifeHP - actualDamage);
            bool wasKilled = newStats.LifeHP <= 0;

            return new DamageResult(
                newStats,
                totalDamageToArmour: 0,
                totalDamageToLife: actualDamage,
                wasKilled: wasKilled,
                damageByType: new Dictionary<DamageType, int>());
        }

        /// <summary>
        /// Calculates accuracy for an attack based on tile tier and status modifiers.
        /// Per COMBAT_MECHANICS.md: base accuracy varies by tile, modified by statuses.
        /// </summary>
        public float CalculateAccuracy(
            int attackerTileTier,
            int defenderTileTier,
            UnitStats attackerStats,
            UnitStats defenderStats)
        {
            // Base accuracy from tile tier difference (adjust per COMBAT_MECHANICS)
            // Example: Tier 1 base 75%, Tier 2 base 85%, Tier 3 base 90%
            float baseAccuracy = attackerTileTier switch
            {
                1 => 0.75f,
                2 => 0.85f,
                3 => 0.90f,
                _ => 0.85f
            };

            // Tile tier advantage/disadvantage
            int tierDiff = attackerTileTier - defenderTileTier;
            float tierModifier = tierDiff * 0.05f; // +5% per tier advantage

            // Status modifiers (example - adjust per COMBAT_MECHANICS)
            float statusModifier = 0f;
            foreach (var status in attackerStats.Statuses)
            {
                if (status.StatusId == "concussed")
                    statusModifier -= 0.15f; // -15% accuracy when concussed
                // Add other status effects here (e.g., Freezing reduces accuracy)
            }

            foreach (var status in defenderStats.Statuses)
            {
                if (status.StatusId == "frozen")
                    statusModifier += 0.25f; // +25% accuracy vs frozen target (auto-hit in some cases)
            }

            return Math.Clamp(baseAccuracy + tierModifier + statusModifier, 0f, 1f);
        }
    }
}
