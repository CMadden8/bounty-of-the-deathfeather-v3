using System;

namespace TurnBasedStrategyFramework.Common.Utilities
{
    public readonly struct Vector2IntImpl : IVector2Int
    {
        public readonly int x { get; }
        public readonly int y { get; }

        public Vector2IntImpl(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public readonly IVector2Int Add(IVector2Int other)
        {
            return new Vector2IntImpl(x + other.x, y + other.y);
        }

        public readonly IVector2Int Subtract(IVector2Int value)
        {
            return new Vector2IntImpl(x - value.x, y - value.y);
        }

        public readonly float Dot(IVector2Int other)
        {
            return x * other.x + y * other.y;
        }

        public readonly IVector2Int Normalize()
        {
            throw new NotImplementedException();
        }

        public readonly override bool Equals(object other)
        {
            if (other is not IVector2Int)
            {
                return false;
            }

            return Equals((IVector2Int)other);
        }

        public readonly bool Equals(IVector2Int other)
        {
            return other.x == x && other.y == y;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }
        public readonly override string ToString()
        {
            return $"({x}, {y})";
        }

        public static bool operator ==(Vector2IntImpl left, Vector2IntImpl right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector2IntImpl left, Vector2IntImpl right)
        {
            return !(left == right);
        }
    }
}