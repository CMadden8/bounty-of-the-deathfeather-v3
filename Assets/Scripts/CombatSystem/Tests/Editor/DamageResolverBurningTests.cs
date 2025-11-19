using NUnit.Framework;
using BountyOfTheDeathfeather.CombatSystem;
using System.Collections.Generic;

namespace BountyOfTheDeathfeather.Tests.CombatSystem
{
    [TestFixture]
    public class DamageResolverBurningTests
    {
        [Test]
        public void ResolveBurningTick_DamageToRandomPool_WhenArmourExists()
        {
            // Arrange
            // RNG returns 0.0 -> index 0.
            // Available pools: Piercing(10), Slashing(10), Bludgeoning(10).
            // Index 0 should be Piercing (order depends on list construction, usually insertion order).
            var rng = new DeterministicRandomProvider(0.0f);
            var resolver = new DamageResolver(rng);

            var armour = new ArmourPools(10, 10, 10);
            var unit = new UnitStats(20, 20, armour, armour, 2, 2, 5, 5);
            int damage = 2;

            // Act
            var result = resolver.ResolveBurningTick(unit, damage);

            // Assert
            Assert.AreEqual(8, result.FinalStats.Armour.Piercing, "Piercing should take 2 damage");
            Assert.AreEqual(10, result.FinalStats.Armour.Slashing);
            Assert.AreEqual(10, result.FinalStats.Armour.Bludgeoning);
            Assert.AreEqual(20, result.FinalStats.LifeHP);
            Assert.AreEqual(2, result.TotalDamageToArmour);
            Assert.AreEqual(0, result.TotalDamageToLife);
        }

        [Test]
        public void ResolveBurningTick_DamageToLife_WhenArmourDepleted()
        {
            // Arrange
            var rng = new DeterministicRandomProvider(0.0f);
            var resolver = new DamageResolver(rng);

            var armour = new ArmourPools(0, 0, 0);
            var unit = new UnitStats(20, 20, armour, armour, 2, 2, 5, 5);
            int damage = 2;

            // Act
            var result = resolver.ResolveBurningTick(unit, damage);

            // Assert
            Assert.AreEqual(0, result.FinalStats.Armour.Piercing);
            Assert.AreEqual(18, result.FinalStats.LifeHP, "Life should take 2 damage");
            Assert.AreEqual(0, result.TotalDamageToArmour);
            Assert.AreEqual(2, result.TotalDamageToLife);
        }

        [Test]
        public void ResolveBurningTick_SpilloverToLife_WhenPoolDepleted()
        {
            // Arrange
            // Piercing has 1 HP. Damage is 2.
            // RNG selects Piercing.
            var rng = new DeterministicRandomProvider(0.0f);
            var resolver = new DamageResolver(rng);

            var armour = new ArmourPools(1, 0, 0);
            var unit = new UnitStats(20, 20, armour, armour, 2, 2, 5, 5);
            int damage = 2;

            // Act
            var result = resolver.ResolveBurningTick(unit, damage);

            // Assert
            Assert.AreEqual(0, result.FinalStats.Armour.Piercing);
            Assert.AreEqual(19, result.FinalStats.LifeHP, "Should spill 1 damage to Life");
            Assert.AreEqual(1, result.TotalDamageToArmour);
            Assert.AreEqual(1, result.TotalDamageToLife);
        }
    }
}
