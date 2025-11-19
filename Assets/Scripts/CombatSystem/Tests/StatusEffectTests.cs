using NUnit.Framework;
using BountyOfTheDeathfeather.CombatSystem;

namespace BountyOfTheDeathfeather.Tests.CombatSystem
{
    /// <summary>
    /// Unit tests for StatusEffect class.
    /// Tests immutability, duration management, and expiration logic per COMBAT_MECHANICS.md.
    /// </summary>
    [TestFixture]
    public class StatusEffectTests
    {
        [Test]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var status = new StatusEffect("burning", stacks: 3, duration: 5);

            // Assert
            Assert.AreEqual("burning", status.StatusId);
            Assert.AreEqual(3, status.Stacks);
            Assert.AreEqual(5, status.Duration);
        }

        [Test]
        public void Constructor_NegativeStacks_ClampsToZero()
        {
            // Arrange & Act
            var status = new StatusEffect("frozen", stacks: -2, duration: 3);

            // Assert
            Assert.AreEqual(0, status.Stacks, "Negative stacks should be clamped to 0");
        }

        [Test]
        public void IsExpired_WithZeroDuration_ReturnsTrue()
        {
            // Arrange
            var status = new StatusEffect("concussed", stacks: 1, duration: 0);

            // Act & Assert
            Assert.IsTrue(status.IsExpired, "Should be expired when duration = 0");
        }

        [Test]
        public void IsExpired_WithPositiveDuration_ReturnsFalse()
        {
            // Arrange
            var status = new StatusEffect("poisoned", stacks: 1, duration: 2);

            // Act & Assert
            Assert.IsFalse(status.IsExpired, "Should not be expired when duration > 0");
        }

        [Test]
        public void IsExpired_WithPermanentDuration_ReturnsFalse()
        {
            // Arrange
            var status = new StatusEffect("empowered", stacks: 1, duration: -1);

            // Act & Assert
            Assert.IsFalse(status.IsExpired, "Permanent status (-1 duration) should never expire");
        }

        [Test]
        public void DecrementDuration_WithPositiveDuration_ReducesByOne()
        {
            // Arrange
            var status = new StatusEffect("freezing", stacks: 2, duration: 3);

            // Act
            var decremented = status.DecrementDuration();

            // Assert
            Assert.AreEqual(3, status.Duration, "Original should be unchanged");
            Assert.AreEqual(2, decremented.Duration, "Decremented should have duration - 1");
            Assert.AreEqual(status.Stacks, decremented.Stacks, "Stacks unchanged");
        }

        [Test]
        public void DecrementDuration_WithPermanent_NoChange()
        {
            // Arrange
            var status = new StatusEffect("blessed", stacks: 1, duration: -1);

            // Act
            var decremented = status.DecrementDuration();

            // Assert
            Assert.AreEqual(-1, decremented.Duration, "Permanent duration unchanged");
        }

        [Test]
        public void DecrementDuration_WithZeroDuration_NoChange()
        {
            // Arrange
            var status = new StatusEffect("stunned", stacks: 1, duration: 0);

            // Act
            var decremented = status.DecrementDuration();

            // Assert
            Assert.AreEqual(0, decremented.Duration, "Already expired, no change");
        }

        [Test]
        public void WithStacks_CreatesNewInstance_Immutable()
        {
            // Arrange
            var original = new StatusEffect("burning", stacks: 2, duration: 4);

            // Act
            var modified = original.WithStacks(5);

            // Assert
            Assert.AreEqual(2, original.Stacks, "Original unchanged");
            Assert.AreEqual(5, modified.Stacks, "Modified has new stacks");
            Assert.AreEqual(original.Duration, modified.Duration, "Duration unchanged");
            Assert.AreEqual(original.StatusId, modified.StatusId, "StatusId unchanged");
        }

        [Test]
        public void ToString_FormatsCorrectly()
        {
            // Arrange
            var finite = new StatusEffect("burning", stacks: 3, duration: 2);
            var permanent = new StatusEffect("cursed", stacks: 1, duration: -1);

            // Act
            string finiteStr = finite.ToString();
            string permanentStr = permanent.ToString();

            // Assert
            Assert.AreEqual("Status(burning, Stacks:3, Duration:2)", finiteStr);
            Assert.AreEqual("Status(cursed, Stacks:1, Duration:∞)", permanentStr);
        }
    }
}
