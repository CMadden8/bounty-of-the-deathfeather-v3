using TurnBasedStrategyFramework.Common.Utilities;
using UnityEngine;

namespace TurnBasedStrategyFramework.Unity.Utilities
{
    /// <summary>
    /// Extension methods for converting IVector3 to UnityEngine.Vector3.
    /// </summary>
    public static class IVector3Extension
    {
        public static Vector3 ToVector3(this IVector3 vector)
        {
            return new Vector3(vector.x, vector.y, vector.z);
        }
    }
}