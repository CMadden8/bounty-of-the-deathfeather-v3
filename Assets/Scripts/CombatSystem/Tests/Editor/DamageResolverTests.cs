using NUnit.Framework;
using BountyOfTheDeathfeather.CombatSystem;
using System.Collections.Generic;

namespace BountyOfTheDeathfeather.Tests.CombatSystem
{
    /// <summary>
    /// Deterministic RNG for testing - always returns predictable values.
    /// </summary>
    public class TestDeterministicRandomProvider : IRandomProvider
    {
        private readonly float[] _sequence;
        private int _index;

        public TestDeterministicRandomProvider(params float[] sequence)
        {
            _sequence = sequence;
            _index = 0;
        }

        public float Value()
        {
            if (_sequence.Length == 0) return 0.5f;
            float value = _sequence[_index];
            _index = (_index + 1) % _sequence.Length;
            return value;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            float normalized = Value();
            return minInclusive + (int)(normalized * (maxExclusive - minInclusive));
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            float normalized = Value();
            return minInclusive + normalized * (maxInclusive - minInclusive);
        }
    }

    /// <summary>
    /// Unit tests for DamageResolver service.
    /// Tests multi-component damage, accuracy, armour overflow, bypass, critical hits per COMBAT_MECHANICS.md.
    /// </summary>
    [TestFixture]
    public class DamageResolverTests
    {
        [Test]
        public void ResolveAttack_Miss_NoDamageApplied()
        {
            // Arrange: RNG returns 0.95 (miss, since base accuracy 0.85)
            var rng = new TestDeterministicRandomProvider(0.95f);
            var resolver = new DamageResolver(rng);

            var attacker = CreateTestUnit();
            var defender = CreateTestUnit();
            var context = new DamageContext(attacker, defender);

            var components = new List<DamageComponent>
            {
                new DamageComponent(DamageType.Piercing, 10)
            };

            // Act
            var result = resolver.ResolveAttack(components, context);

            // Assert
            Assert.AreEqual(defender.LifeHP, result.FinalStats.LifeHP, "Life HP unchanged on miss");
            Assert.AreEqual(defender.Armour, result.FinalStats.Armour, "Armour unchanged on miss");
            Assert.AreEqual(0, result.TotalDamageToArmour, "No armour damage on miss");
            Assert.AreEqual(0, result.TotalDamageToLife, "No life damage on miss");
            Assert.IsFalse(result.WasKilled, "Unit not killed on miss");
        }

        [Test]
        public void ResolveAttack_Hit_DamagesArmourBypass()
        {
            // Arrange: RNG returns 0.5 (hit)
            var rng = new TestDeterministicRandomProvider(0.5f);
            var resolver = new DamageResolver(rng);

            var attacker = CreateTestUnit();
            var defender = CreateTestUnit(); // 10/10/10 armour, 20 Life
            var context = new DamageContext(attacker, defender);

            var components = new List<DamageComponent>
            {
                new DamageComponent(DamageType.Piercing, 5)
            };

            // Act
            var result = resolver.ResolveAttack(components, context);

            // Assert
            Assert.AreEqual(5, result.FinalStats.Armour.Piercing, "Piercing pool reduced by 5");
            Assert.AreEqual(10, result.FinalStats.Armour.Slashing, "Slashing unchanged");
            Assert.AreEqual(10, result.FinalStats.Armour.Bludgeoning, "Bludgeoning unchanged");
            Assert.AreEqual(20, result.FinalStats.LifeHP, "Life HP unchanged (armour absorbed)");
            Assert.AreEqual(5, result.TotalDamageToArmour, "5 damage to armour");
            Assert.AreEqual(0, result.TotalDamageToLife, "No life damage");
            Assert.IsFalse(result.WasKilled);
        }

        [Test]
        public void ResolveAttack_MultiComponent_ResolvesInOrder()
        {
            // Arrange: Hit with multiple damage types
            var rng = new DeterministicRandomProvider(0.5f);
            var resolver = new DamageResolver(rng);

            var attacker = CreateTestUnit();
            var defender = CreateTestUnit(); // 10/10/10 armour
            var context = new DamageContext(attacker, defender);

            var components = new List<DamageComponent>
            {
                new DamageComponent(DamageType.Slashing, 3),
                new DamageComponent(DamageType.Piercing, 4),
                new DamageComponent(DamageType.Bludgeoning, 2)
            };

            // Act (should resolve Piercing→Slashing→Bludgeoning order)
            var result = resolver.ResolveAttack(components, context);

            // Assert
            Assert.AreEqual(6, result.FinalStats.Armour.Piercing, "Piercing: 10 - 4 = 6");
            Assert.AreEqual(7, result.FinalStats.Armour.Slashing, "Slashing: 10 - 3 = 7");
            Assert.AreEqual(8, result.FinalStats.Armour.Bludgeoning, "Bludgeoning: 10 - 2 = 8");
            Assert.AreEqual(9, result.TotalDamageToArmour, "Total 9 damage to armour");
            Assert.AreEqual(0, result.TotalDamageToLife, "No life damage yet");
        }

        [Test]
        public void ResolveAttack_ArmourOverflow_SpillsToLife()
        {
            // Arrange: Damage exceeds one pool, but other pools remain
            var rng = new DeterministicRandomProvider(0.5f);
            var resolver = new DamageResolver(rng);

            var attacker = CreateTestUnit();
            var defenderArmour = new ArmourPools(piercing: 3, slashing: 10, bludgeoning: 10);
            var defender = CreateTestUnit(armour: defenderArmour);
            var context = new DamageContext(attacker, defender);

            var components = new List<DamageComponent>
            {
                new DamageComponent(DamageType.Piercing, 8) // Exceeds piercing pool by 5
            };

            // Act
            var result = resolver.ResolveAttack(components, context);

            // Assert
            Assert.AreEqual(0, result.FinalStats.Armour.Piercing, "Piercing depleted");
            Assert.AreEqual(3, result.TotalDamageToArmour, "Only 3 damage absorbed by armour");
            // Per COMBAT_MECHANICS: overflow does NOT spill to Life HP unless ALL armour depleted
            Assert.AreEqual(20, result.FinalStats.LifeHP, "Life HP unchanged (other pools still have armour)");
            Assert.AreEqual(0, result.TotalDamageToLife, "No life damage (armour not fully depleted)");
        }

        [Test]
        public void ResolveAttack_AllArmourDepleted_OverflowDamagesLife()
        {
            // Arrange: All armour pools depleted, overflow goes to Life
            var rng = new DeterministicRandomProvider(0.5f);
            var resolver = new DamageResolver(rng);

            var attacker = CreateTestUnit();
            var defenderArmour = new ArmourPools(piercing: 2, slashing: 0, bludgeoning: 0);
            var defender = CreateTestUnit(armour: defenderArmour, lifeHP: 20);
            var context = new DamageContext(attacker, defender);

            var components = new List<DamageComponent>
            {
                new DamageComponent(DamageType.Piercing, 7) // 2 to armour, 5 overflow
            };

            // Act
            var result = resolver.ResolveAttack(components, context);

            // Assert
            Assert.AreEqual(0, result.FinalStats.Armour.Piercing, "Piercing depleted");
            Assert.IsTrue(result.FinalStats.Armour.IsFullyDepleted, "All armour depleted");
            Assert.AreEqual(2, result.TotalDamageToArmour, "2 damage to armour");
            Assert.AreEqual(5, result.TotalDamageToLife, "5 overflow damage to Life HP");
            Assert.AreEqual(15, result.FinalStats.LifeHP, "Life HP: 20 - 5 = 15");
            Assert.IsFalse(result.WasKilled);
        }

        [Test]
        public void ResolveAttack_CriticalHit_DealsExtraDamage()
        {
            // Arrange: RNG returns 0.96 (critical hit, >= 0.95 threshold and < accuracy 0.85 + modifier)
            // To ensure hit AND critical, we need accuracy roll within [accuracyThreshold, criticalThreshold)
            // Base accuracy = 0.85, critical = 0.95. Roll 0.80 will hit but not crit.
            // Roll 0.96 will hit (if accuracy >= 0.96) OR we adjust accuracy.
            // Let's set accuracy modifier to +0.15 so final accuracy = 1.0, then roll 0.96 is a hit AND critical.
            var rng = new TestDeterministicRandomProvider(0.96f);
            var resolver = new DamageResolver(rng);

            var attacker = CreateTestUnit();
            var defender = CreateTestUnit(); // 10 piercing armour
            var context = new DamageContext(attacker, defender);
            context.AccuracyModifier = 0.15f; // Final accuracy = 0.85 + 0.15 = 1.0

            var components = new List<DamageComponent>
            {
                new DamageComponent(DamageType.Piercing, 10)
            };

            // Act
            var result = resolver.ResolveAttack(components, context);

            // Assert
            Assert.IsTrue(context.IsCritical, "Should be a critical hit");
            // Critical damage = 10 * 1.5 = 15
            Assert.AreEqual(0, result.FinalStats.Armour.Piercing, "15 damage depletes 10 piercing armour");
            Assert.AreEqual(10, result.TotalDamageToArmour, "10 absorbed by armour");
            // 5 overflow, but other pools not depleted, so no life damage
            Assert.AreEqual(0, result.TotalDamageToLife, "No life damage (other pools remain)");
        }

        [Test]
        public void ResolveAttack_BypassArmour_DirectLifeDamage()
        {
            // Arrange: Poison bypasses armour
            var rng = new TestDeterministicRandomProvider(0.5f);
            var resolver = new DamageResolver(rng);

            var attacker = CreateTestUnit();
            var defender = CreateTestUnit(); // 10/10/10 armour, 20 Life
            var context = new DamageContext(attacker, defender);
            context.BypassArmour = true;

            var components = new List<DamageComponent>
            {
                new DamageComponent(DamageType.Piercing, 5)
            };

            // Act
            var result = resolver.ResolveAttack(components, context);

            // Assert
            Assert.AreEqual(defender.Armour, result.FinalStats.Armour, "Armour unchanged (bypassed)");
            Assert.AreEqual(15, result.FinalStats.LifeHP, "Life HP: 20 - 5 = 15");
            Assert.AreEqual(0, result.TotalDamageToArmour, "No armour damage");
            Assert.AreEqual(5, result.TotalDamageToLife, "5 direct life damage");
            Assert.IsFalse(result.WasKilled);
        }

        [Test]
        public void ResolveAttack_KillsUnit_WhenLifeReachesZero()
        {
            // Arrange: Lethal damage
            var rng = new TestDeterministicRandomProvider(0.5f);
            var resolver = new DamageResolver(rng);

            var attacker = CreateTestUnit();
            var defenderArmour = new ArmourPools(0, 0, 0); // No armour
            var defender = CreateTestUnit(armour: defenderArmour, lifeHP: 5);
            var context = new DamageContext(attacker, defender);

            var components = new List<DamageComponent>
            {
                new DamageComponent(DamageType.Piercing, 10)
            };

            // Act
            var result = resolver.ResolveAttack(components, context);

            // Assert
            Assert.AreEqual(0, result.FinalStats.LifeHP, "Life HP reduced to 0");
            Assert.IsTrue(result.WasKilled, "Unit was killed");
            Assert.AreEqual(5, result.TotalDamageToLife, "Only 5 damage applied (capped at current Life)");
        }

        [Test]
        public void ApplyDirectLifeDamage_BypassesAccuracyAndArmour()
        {
            // Arrange: Direct damage (e.g., Poison tick, falling)
            var rng = new TestDeterministicRandomProvider(0.99f); // RNG not used
            var resolver = new DamageResolver(rng);

            var defender = CreateTestUnit(); // 10/10/10 armour, 20 Life

            // Act
            var result = resolver.ApplyDirectLifeDamage(defender, 8);

            // Assert
            Assert.AreEqual(defender.Armour, result.FinalStats.Armour, "Armour unchanged");
            Assert.AreEqual(12, result.FinalStats.LifeHP, "Life HP: 20 - 8 = 12");
            Assert.AreEqual(0, result.TotalDamageToArmour, "No armour damage");
            Assert.AreEqual(8, result.TotalDamageToLife, "8 direct life damage");
            Assert.IsFalse(result.WasKilled);
        }

        [Test]
        public void CalculateAccuracy_TierAdvantage_IncreasesAccuracy()
        {
            // Arrange
            var rng = new TestDeterministicRandomProvider(0.5f);
            var resolver = new DamageResolver(rng);
            var attacker = CreateTestUnit();
            var defender = CreateTestUnit();

            // Act: Attacker on tier 3, defender on tier 1
            float accuracy = resolver.CalculateAccuracy(
                attackerTileTier: 3,
                defenderTileTier: 1,
                attackerStats: attacker,
                defenderStats: defender);

            // Assert
            // Base tier 3 = 0.90, tier advantage = +2 * 0.05 = +0.10 => 1.00
            // Allow a small tolerance for float rounding differences across platforms/runtime.
            Assert.AreEqual(1.0f, accuracy, 1e-6f, "Tier 3 with +2 tier advantage = 100% accuracy");
        }

        [Test]
        public void CalculateAccuracy_StatusModifiers_AppliedCorrectly()
        {
            // Arrange
            var rng = new TestDeterministicRandomProvider(0.5f);
            var resolver = new DamageResolver(rng);

            var attackerStatuses = new List<StatusEffect>
            {
                new StatusEffect("concussed", stacks: 1)
            };
            var attacker = CreateTestUnit(statuses: attackerStatuses);

            var defenderStatuses = new List<StatusEffect>
            {
                new StatusEffect("frozen", stacks: 1)
            };
            var defender = CreateTestUnit(statuses: defenderStatuses);

            // Act
            float accuracy = resolver.CalculateAccuracy(
                attackerTileTier: 2,
                defenderTileTier: 2,
                attackerStats: attacker,
                defenderStats: defender);

            // Assert
            // Base tier 2 = 0.85, concussed = -0.15, frozen target = +0.25
            // Total = 0.85 - 0.15 + 0.25 = 0.95
            // Allow a small tolerance for float rounding differences across platforms/runtime.
            Assert.AreEqual(0.95f, accuracy, 1e-6f, "Accuracy with status modifiers");
        }

        private UnitStats CreateTestUnit(
            ArmourPools? armour = null,
            int lifeHP = 20,
            IReadOnlyList<StatusEffect> statuses = null)
        {
            var defaultArmour = armour ?? new ArmourPools(10, 10, 10);
            return new UnitStats(
                lifeHP: lifeHP,
                maxLifeHP: 20,
                armour: defaultArmour,
                maxArmour: defaultArmour,
                actionPoints: 2,
                maxActionPoints: 2,
                movementPoints: 5,
                maxMovementPoints: 5,
                statuses: statuses);
        }
    }
}
