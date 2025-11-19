using NUnit.Framework;
using BountyOfTheDeathfeather.CombatSystem;

namespace BountyOfTheDeathfeather.Tests.CombatSystem
{
    /// <summary>
    /// Unit tests for ArmourPools struct.
    /// Tests overflow calculation, immutability, and depletion checks per COMBAT_MECHANICS.md.
    /// </summary>
    [TestFixture]
    public class ArmourPoolsTests
    {
        [Test]
        public void ApplyDamage_WithinPool_ReturnsNewPoolsWithZeroOverflow()
        {
            // Arrange
            var pools = new ArmourPools(piercing: 10, slashing: 8, bludgeoning: 6);

            // Act
            var (newPools, overflow) = pools.ApplyDamage(DamageType.Piercing, 5);

            // Assert
            Assert.AreEqual(5, newPools.Piercing, "Piercing pool should be reduced by 5");
            Assert.AreEqual(8, newPools.Slashing, "Slashing pool should be unchanged");
            Assert.AreEqual(6, newPools.Bludgeoning, "Bludgeoning pool should be unchanged");
            Assert.AreEqual(0, overflow, "Overflow should be zero when damage within pool");
        }

        [Test]
        public void ApplyDamage_ExceedsPool_ReturnsDepletedPoolAndOverflow()
        {
            // Arrange
            var pools = new ArmourPools(piercing: 3, slashing: 5, bludgeoning: 4);

            // Act
            var (newPools, overflow) = pools.ApplyDamage(DamageType.Slashing, 7);

            // Assert
            Assert.AreEqual(0, newPools.Slashing, "Slashing pool should be depleted to 0");
            Assert.AreEqual(2, overflow, "Overflow should be 2 (7 - 5)");
            Assert.AreEqual(3, newPools.Piercing, "Other pools unchanged");
            Assert.AreEqual(4, newPools.Bludgeoning, "Other pools unchanged");
        }

        [Test]
        public void ApplyDamage_NegativeDamage_NoChange()
        {
            // Arrange
            var pools = new ArmourPools(piercing: 5, slashing: 5, bludgeoning: 5);

            // Act
            var (newPools, overflow) = pools.ApplyDamage(DamageType.Bludgeoning, -3);

            // Assert
            Assert.AreEqual(pools, newPools, "Pools should be unchanged with negative damage");
            Assert.AreEqual(0, overflow, "Overflow should be zero");
        }

        [Test]
        public void IsFullyDepleted_AllPoolsZero_ReturnsTrue()
        {
            // Arrange
            var pools = new ArmourPools(0, 0, 0);

            // Act & Assert
            Assert.IsTrue(pools.IsFullyDepleted, "Should be fully depleted when all pools are 0");
        }

        [Test]
        public void IsFullyDepleted_AnyPoolNonZero_ReturnsFalse()
        {
            // Arrange
            var pools1 = new ArmourPools(1, 0, 0);
            var pools2 = new ArmourPools(0, 1, 0);
            var pools3 = new ArmourPools(0, 0, 1);

            // Act & Assert
            Assert.IsFalse(pools1.IsFullyDepleted, "Not fully depleted with piercing > 0");
            Assert.IsFalse(pools2.IsFullyDepleted, "Not fully depleted with slashing > 0");
            Assert.IsFalse(pools3.IsFullyDepleted, "Not fully depleted with bludgeoning > 0");
        }

        [Test]
        public void TotalArmour_CalculatesCorrectSum()
        {
            // Arrange
            var pools = new ArmourPools(piercing: 12, slashing: 8, bludgeoning: 5);

            // Act
            int total = pools.TotalArmour;

            // Assert
            Assert.AreEqual(25, total, "Total armour should be sum of all pools");
        }

        [Test]
        public void Constructor_NegativeValues_ClampsToZero()
        {
            // Arrange & Act
            var pools = new ArmourPools(piercing: -5, slashing: 10, bludgeoning: -2);

            // Assert
            Assert.AreEqual(0, pools.Piercing, "Negative piercing should be clamped to 0");
            Assert.AreEqual(10, pools.Slashing, "Positive slashing unchanged");
            Assert.AreEqual(0, pools.Bludgeoning, "Negative bludgeoning should be clamped to 0");
        }

        [Test]
        public void ApplyDamage_Immutability_OriginalUnchanged()
        {
            // Arrange
            var original = new ArmourPools(piercing: 10, slashing: 10, bludgeoning: 10);

            // Act
            var (newPools, _) = original.ApplyDamage(DamageType.Piercing, 5);

            // Assert
            Assert.AreEqual(10, original.Piercing, "Original should be unchanged (immutable)");
            Assert.AreEqual(5, newPools.Piercing, "New pools should reflect damage");
        }
    }
}
