using UnityEditor;
using UnityEngine;
using TurnBasedStrategyFramework.Unity.Units;

namespace CombatPOC.Editor
{
    /// <summary>
    /// Quick utility to update movement animation speed on existing units in the scene.
    /// </summary>
    public static class UpdateUnitSpeed
    {
        [MenuItem("Tools/Combat POC/Update Unit Movement Speed")]
        public static void UpdateAllUnitsSpeed()
        {
            var units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
            int count = 0;
            
            foreach (var unit in units)
            {
                if (unit.MovementAnimationSpeed != 2f)
                {
                    Undo.RecordObject(unit, "Update Movement Speed");
                    unit.MovementAnimationSpeed = 2f;
                    EditorUtility.SetDirty(unit);
                    count++;
                    Debug.Log($"Updated {unit.name} movement speed to 2.0");
                }
            }
            
            if (count > 0)
            {
                Debug.Log($"Updated movement speed on {count} unit(s).");
            }
            else
            {
                Debug.Log("All units already have movement speed = 2.0");
            }
        }
    }
}
