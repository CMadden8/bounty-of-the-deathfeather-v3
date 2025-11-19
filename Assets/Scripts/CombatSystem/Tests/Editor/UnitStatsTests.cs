using NUnit.Framework;
using BountyOfTheDeathfeather.CombatSystem;
using System.Collections.Generic;

namespace BountyOfTheDeathfeather.Tests.CombatSystem
{
    /// <summary>
    /// Unit tests for UnitStats class.
    /// Tests immutability, state transitions, and lifecycle methods per COMBAT_MECHANICS.md.
    /// </summary>
    [TestFixture]
    public class UnitStatsTests
    {
        [Test]
        public void WithLifeHP_CreatesNewInstance_OriginalUnchanged()
        {
            // Arrange
            var armour = new ArmourPools(5, 5, 5);
            var original = new UnitStats(
                lifeHP: 20,
                maxLifeHP: 20,
                armour: armour,
                maxArmour: armour,
                actionPoints: 2,
                maxActionPoints: 2,
                movementPoints: 5,
                maxMovementPoints: 5);

            // Act
            var modified = original.WithLifeHP(10);

            // Assert
            Assert.AreEqual(20, original.LifeHP, "Original should be unchanged");
            Assert.AreEqual(10, modified.LifeHP, "Modified should have new Life HP");
        }

        [Test]
        public void WithArmour_CreatesNewInstance_OtherFieldsUnchanged()
        {
            // Arrange
            var originalArmour = new ArmourPools(10, 10, 10);
            var stats = new UnitStats(
                lifeHP: 20,
                maxLifeHP: 20,
                armour: originalArmour,
                maxArmour: originalArmour,
                actionPoints: 2,
                maxActionPoints: 2,
                movementPoints: 5,
                maxMovementPoints: 5);

            // Act
            var newArmour = new ArmourPools(5, 8, 7);
            var modified = stats.WithArmour(newArmour);

            // Assert
            Assert.AreEqual(originalArmour, stats.Armour, "Original armour unchanged");
            Assert.AreEqual(newArmour, modified.Armour, "Modified has new armour");
            Assert.AreEqual(stats.LifeHP, modified.LifeHP, "Life HP unchanged");
            Assert.AreEqual(stats.ActionPoints, modified.ActionPoints, "AP unchanged");
        }

        [Test]
        public void IsAlive_WithPositiveLifeHP_ReturnsTrue()
        {
            // Arrange
            var stats = CreateDefaultStats(lifeHP: 10);

            // Act & Assert
            Assert.IsTrue(stats.IsAlive, "Should be alive with Life HP > 0");
        }

        [Test]
        public void IsAlive_WithZeroLifeHP_ReturnsFalse()
        {
            // Arrange
            var stats = CreateDefaultStats(lifeHP: 0);

            // Act & Assert
            Assert.IsFalse(stats.IsAlive, "Should not be alive with Life HP = 0");
        }

        [Test]
        public void CanAct_WithAPAndAlive_ReturnsTrue()
        {
            // Arrange
            var stats = CreateDefaultStats(lifeHP: 10, actionPoints: 1);

            // Act & Assert
            Assert.IsTrue(stats.CanAct, "Should be able to act with AP > 0 and alive");
        }

        [Test]
        public void CanAct_WithZeroAP_ReturnsFalse()
        {
            // Arrange
            var stats = CreateDefaultStats(lifeHP: 10, actionPoints: 0);

            // Act & Assert
            Assert.IsFalse(stats.CanAct, "Should not be able to act with AP = 0");
        }

        [Test]
        public void CanAct_WhenDead_ReturnsFalse()
        {
            // Arrange
            var stats = CreateDefaultStats(lifeHP: 0, actionPoints: 2);

            // Act & Assert
            Assert.IsFalse(stats.CanAct, "Should not be able to act when dead");
        }

        [Test]
        public void CanMove_WithMPAndAlive_ReturnsTrue()
        {
            // Arrange
            var stats = CreateDefaultStats(lifeHP: 10, movementPoints: 3);

            // Act & Assert
            Assert.IsTrue(stats.CanMove, "Should be able to move with MP > 0 and alive");
        }

        [Test]
        public void CanMove_WithZeroMP_ReturnsFalse()
        {
            // Arrange
            var stats = CreateDefaultStats(lifeHP: 10, movementPoints: 0);

            // Act & Assert
            Assert.IsFalse(stats.CanMove, "Should not be able to move with MP = 0");
        }

        [Test]
        public void ResetForTurnStart_RestoresAPAndMP()
        {
            // Arrange
            var stats = CreateDefaultStats(
                actionPoints: 0,
                movementPoints: 1);

            // Act
            var reset = stats.ResetForTurnStart();

            // Assert
            Assert.AreEqual(2, reset.ActionPoints, "AP should be reset to max");
            Assert.AreEqual(5, reset.MovementPoints, "MP should be reset to max");
            Assert.AreEqual(stats.LifeHP, reset.LifeHP, "Life HP unchanged by reset");
        }

        [Test]
        public void Constructor_NegativeValues_ClampsToZero()
        {
            // Arrange & Act
            var stats = new UnitStats(
                lifeHP: -5,
                maxLifeHP: 10,
                armour: new ArmourPools(0, 0, 0),
                maxArmour: new ArmourPools(5, 5, 5),
                actionPoints: -1,
                maxActionPoints: 2,
                movementPoints: -3,
                maxMovementPoints: 5);

            // Assert
            Assert.AreEqual(0, stats.LifeHP, "Negative Life HP clamped to 0");
            Assert.AreEqual(0, stats.ActionPoints, "Negative AP clamped to 0");
            Assert.AreEqual(0, stats.MovementPoints, "Negative MP clamped to 0");
        }

        [Test]
        public void WithStatuses_UpdatesStatusList()
        {
            // Arrange
            var stats = CreateDefaultStats();
            var statuses = new List<StatusEffect>
            {
                new StatusEffect("burning", stacks: 2, duration: 3),
                new StatusEffect("poisoned", stacks: 1, duration: -1)
            };

            // Act
            var modified = stats.WithStatuses(statuses);

            // Assert
            Assert.AreEqual(0, stats.Statuses.Count, "Original has no statuses");
            Assert.AreEqual(2, modified.Statuses.Count, "Modified has 2 statuses");
            Assert.AreEqual("burning", modified.Statuses[0].StatusId);
            Assert.AreEqual("poisoned", modified.Statuses[1].StatusId);
        }

        private UnitStats CreateDefaultStats(
            int lifeHP = 20,
            int actionPoints = 2,
            int movementPoints = 5)
        {
            var armour = new ArmourPools(5, 5, 5);
            return new UnitStats(
                lifeHP: lifeHP,
                maxLifeHP: 20,
                armour: armour,
                maxArmour: armour,
                actionPoints: actionPoints,
                maxActionPoints: 2,
                movementPoints: movementPoints,
                maxMovementPoints: 5);
        }
    }
}
