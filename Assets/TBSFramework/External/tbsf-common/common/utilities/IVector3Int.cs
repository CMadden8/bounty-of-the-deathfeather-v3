using System;

namespace TurnBasedStrategyFramework.Common.Utilities
{
    /// <summary>
    /// Represents a 3D vector with integer values, providing basic vector arithmetic and equality operations.
    /// </summary>
    public interface IVector3Int : IVectorArithmetics<IVector3Int>, IEquatable<IVector3Int>
    {
        /// <summary>
        /// Gets the X-coordinate of the vector.
        /// </summary>
        int x { get; }

        /// <summary>
        /// Gets the Y-coordinate of the vector.
        /// </summary>
        int y { get; }

        /// <summary>
        /// Gets the Z-coordinate of the vector.
        /// </summary>
        int z { get; }
    }
}
