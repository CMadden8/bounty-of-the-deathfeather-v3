namespace BountyOfTheDeathfeather.CombatSystem.Tiles
{
    public enum TileEffectType
    {
        None,
        Flame,
        Ice,
        Shadow
    }

    public class TileEffect
    {
        public TileEffectType Type { get; private set; }
        public int Duration { get; private set; }
        public object Metadata { get; private set; }

        public TileEffect(TileEffectType type, int duration, object metadata = null)
        {
            Type = type;
            Duration = duration;
            Metadata = metadata;
        }

        public TileEffect DecrementDuration()
        {
            return new TileEffect(Type, Duration - 1, Metadata);
        }

        public bool IsExpired => Duration <= 0;
    }
}
