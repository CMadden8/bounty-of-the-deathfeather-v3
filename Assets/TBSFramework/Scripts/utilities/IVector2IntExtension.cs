using TurnBasedStrategyFramework.Common.Utilities;
using UnityEngine;

namespace TurnBasedStrategyFramework.Unity.Utilities
{
    /// <summary>
    /// Extension methods for converting IVector2Int to UnityEngine.Vector2Int.
    /// </summary>
    public static class IVector2IntExtension
    {
        public static Vector2Int ToVector2Int(this IVector2Int vector)
        {
            return new Vector2Int(vector.x, vector.y);
        }
    }
}