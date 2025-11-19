using System;

namespace BountyOfTheDeathfeather.CombatSystem
{
    /// <summary>
    /// Represents an active status effect on a unit (e.g., Burning, Poisoned, Frozen).
    /// Per COMBAT_MECHANICS.md: statuses tick at turn start and have deterministic behavior.
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        public readonly string StatusId; // e.g., "burning", "poisoned"
        public readonly int Stacks; // intensity or stack count (e.g., Freezing stacks)
        public readonly int Duration; // turns remaining (-1 = permanent)
        public readonly object Metadata; // optional data (e.g., damage per tick)

        public StatusEffect(string statusId, int stacks = 1, int duration = -1, object metadata = null)
        {
            StatusId = statusId ?? throw new ArgumentNullException(nameof(statusId));
            Stacks = Math.Max(0, stacks);
            Duration = duration;
            Metadata = metadata;
        }

        /// <summary>
        /// Returns true if this status has expired (Duration == 0).
        /// </summary>
        public bool IsExpired => Duration == 0;

        /// <summary>
        /// Creates a new StatusEffect with stacks modified.
        /// </summary>
        public StatusEffect WithStacks(int newStacks) =>
            new StatusEffect(StatusId, newStacks, Duration, Metadata);

        /// <summary>
        /// Creates a new StatusEffect with duration decremented by 1.
        /// </summary>
        public StatusEffect DecrementDuration() =>
            Duration > 0 ? new StatusEffect(StatusId, Stacks, Duration - 1, Metadata) : this;

        public override string ToString() =>
            $"Status({StatusId}, Stacks:{Stacks}, Duration:{(Duration < 0 ? "∞" : Duration.ToString())})";
    }
}
