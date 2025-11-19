using System;

namespace BountyOfTheDeathfeather.CombatSystem
{
    /// <summary>
    /// Interface for random number generation, injected into services for testability.
    /// Allows seeded/deterministic RNG in tests and true random in production.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>
        /// Returns a random integer in the range [minInclusive, maxExclusive).
        /// </summary>
        int Range(int minInclusive, int maxExclusive);

        /// <summary>
        /// Returns a random float in the range [0.0, 1.0).
        /// </summary>
        float Value();

        /// <summary>
        /// Returns a random float in the range [minInclusive, maxInclusive].
        /// </summary>
        float Range(float minInclusive, float maxInclusive);
    }

    /// <summary>
    /// Production implementation using System.Random with optional seed.
    /// </summary>
    public class SystemRandomProvider : IRandomProvider
    {
        private readonly Random _random;

        public SystemRandomProvider(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public int Range(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
        public float Value() => (float)_random.NextDouble();
        public float Range(float minInclusive, float maxInclusive) =>
            minInclusive + (float)_random.NextDouble() * (maxInclusive - minInclusive);
    }
}
