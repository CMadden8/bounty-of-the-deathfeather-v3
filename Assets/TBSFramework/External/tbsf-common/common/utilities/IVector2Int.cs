using System;

namespace TurnBasedStrategyFramework.Common.Utilities
{
    /// <summary>
    /// Represents a 2D integer vector, providing basic vector arithmetic and equality operations.
    /// </summary>
    public interface IVector2Int : IVectorArithmetics<IVector2Int>, IEquatable<IVector2Int>
    {
        /// <summary>
        /// Gets the X-coordinate of the vector.
        /// </summary>
        int x { get; }

        /// <summary>
        /// Gets the Y-coordinate of the vector.
        /// </summary>
        int y { get; }
    }
}
