using System;

namespace TurnBasedStrategyFramework.Common.Utilities
{
    public readonly struct Vector3IntImpl : IVector3Int
    {
        public readonly int x { get; }
        public readonly int y { get; }
        public readonly int z { get; }

        public Vector3IntImpl(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public readonly IVector3Int Add(IVector3Int other)
        {
            return new Vector3IntImpl(x + other.x, y + other.y, z + other.z);
        }

        public readonly IVector3Int Subtract(IVector3Int other)
        {
            return new Vector3IntImpl(x - other.x, y - other.y, z - other.z);
        }

        public readonly override string ToString()
        {
            return $"({x}, {y}, {z})";
        }

        public readonly override bool Equals(object other)
        {
            if (other is not IVector3Int)
            {
                return false;
            }

            return Equals((IVector3Int)other);
        }

        public readonly bool Equals(IVector3Int other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(x, y, z);
        }

        public float Dot(IVector3Int other)
        {
            return x * other.x + y * other.y + z * other.z;
        }

        public IVector3Int Normalize()
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(Vector3IntImpl left, Vector3IntImpl right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3IntImpl left, Vector3IntImpl right)
        {
            return !(left == right);
        }
    }
}

