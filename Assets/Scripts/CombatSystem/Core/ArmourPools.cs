using System;

namespace BountyOfTheDeathfeather.CombatSystem
{
    /// <summary>
    /// Immutable struct representing the three armour pools (Piercing, Slashing, Bludgeoning).
    /// Per COMBAT_MECHANICS.md: Life HP cannot be damaged until all three pools reach 0.
    /// </summary>
    [Serializable]
    public struct ArmourPools : IEquatable<ArmourPools>
    {
        public readonly int Piercing;
        public readonly int Slashing;
        public readonly int Bludgeoning;

        public ArmourPools(int piercing, int slashing, int bludgeoning)
        {
            Piercing = Math.Max(0, piercing);
            Slashing = Math.Max(0, slashing);
            Bludgeoning = Math.Max(0, bludgeoning);
        }

        /// <summary>
        /// Returns true if all three armour pools are depleted (0).
        /// </summary>
        public bool IsFullyDepleted => Piercing == 0 && Slashing == 0 && Bludgeoning == 0;

        /// <summary>
        /// Returns the total armour across all three pools.
        /// </summary>
        public int TotalArmour => Piercing + Slashing + Bludgeoning;

        /// <summary>
        /// Creates a new ArmourPools with the specified pool reduced by the given amount.
        /// Returns the new pools and the amount of overflow damage (if pool went below 0).
        /// </summary>
        public (ArmourPools newPools, int overflow) ApplyDamage(DamageType type, int amount)
        {
            if (amount <= 0) return (this, 0);

            switch (type)
            {
                case DamageType.Piercing:
                    {
                        int newValue = Piercing - amount;
                        int overflow = newValue < 0 ? -newValue : 0;
                        return (new ArmourPools(Math.Max(0, newValue), Slashing, Bludgeoning), overflow);
                    }
                case DamageType.Slashing:
                    {
                        int newValue = Slashing - amount;
                        int overflow = newValue < 0 ? -newValue : 0;
                        return (new ArmourPools(Piercing, Math.Max(0, newValue), Bludgeoning), overflow);
                    }
                case DamageType.Bludgeoning:
                    {
                        int newValue = Bludgeoning - amount;
                        int overflow = newValue < 0 ? -newValue : 0;
                        return (new ArmourPools(Piercing, Slashing, Math.Max(0, newValue)), overflow);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown damage type");
            }
        }

        public bool Equals(ArmourPools other) =>
            Piercing == other.Piercing && Slashing == other.Slashing && Bludgeoning == other.Bludgeoning;

        public override bool Equals(object obj) => obj is ArmourPools other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Piercing, Slashing, Bludgeoning);

        public static bool operator ==(ArmourPools left, ArmourPools right) => left.Equals(right);
        public static bool operator !=(ArmourPools left, ArmourPools right) => !left.Equals(right);

        public override string ToString() => $"Armour(P:{Piercing}, S:{Slashing}, B:{Bludgeoning})";
    }
}
