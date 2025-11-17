using System;

namespace TurnBasedStrategyFramework.Common.Utilities
{
    public readonly struct Vector3Impl : IVector3
    {
        public readonly float x { get; }
        public readonly float y { get; }
        public readonly float z { get; }

        public Vector3Impl(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public readonly IVector3 Add(IVector3 other)
        {
            return new Vector3Impl(x + other.x, y + other.y, z + other.z);
        }

        public readonly IVector3 Subtract(IVector3 other)
        {
            return new Vector3Impl(x - other.x, y - other.y, z - other.z);
        }

        public readonly override string ToString()
        {
            return $"({x}, {y}, {z})";
        }

        public readonly override bool Equals(object other)
        {
            if (other is not IVector3)
            {
                return false;
            }

            return Equals((IVector3)other);
        }

        public readonly bool Equals(IVector3 other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(x, y, z);
        }
        public float Dot(IVector3 other)
        {
            return x * other.x + y * other.y + z * other.z;
        }

        public IVector3 Normalize()
        {
            float magnitude = (float)Math.Sqrt(x * x + y * y + z * z);
            if (magnitude == 0)
            {
                return new Vector3Impl(0, 0, 0);
            }
            return new Vector3Impl(x / magnitude, y / magnitude, z / magnitude);
        }

        public static bool operator ==(Vector3Impl left, Vector3Impl right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3Impl left, Vector3Impl right)
        {
            return !(left == right);
        }
    }
}