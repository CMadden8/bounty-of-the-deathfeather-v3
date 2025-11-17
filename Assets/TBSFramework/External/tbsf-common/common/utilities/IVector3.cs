using System;

namespace TurnBasedStrategyFramework.Common.Utilities
{
    /// <summary>
    /// Represents a 3D vector with floating-point values, providing basic vector arithmetic and equality operations.
    /// </summary>
    public interface IVector3 : IVectorArithmetics<IVector3>, IEquatable<IVector3>
    {
        /// <summary>
        /// Gets the X-coordinate of the vector.
        /// </summary>
        float x { get; }

        /// <summary>
        /// Gets the Y-coordinate of the vector.
        /// </summary>
        float y { get; }

        /// <summary>
        /// Gets the Z-coordinate of the vector.
        /// </summary>
        float z { get; }
    }
}
