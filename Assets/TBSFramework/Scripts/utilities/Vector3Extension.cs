using TurnBasedStrategyFramework.Common.Utilities;
using UnityEngine;

namespace TurnBasedStrategyFramework.Unity.Utilities
{
    /// <summary>
    /// Extension methods for converting UnityEngine.Vector3 to IVector3.
    /// </summary>
    public static class Vector3Extension
    {
        public static IVector3 ToIVector3(this Vector3 vector)
        {
            return new Vector3Impl(vector.x, vector.y, vector.z);
        }
    }
}
