using System.Linq;
using System.Collections.Generic;
using TurnBasedStrategyFramework.Unity.Controllers;
using TurnBasedStrategyFramework.Unity.Cells;
using UnityEngine;

namespace BountyOfTheDeathfeather.Debugging
{
    /// <summary>
    /// Runtime helper to detect cells that are missing highlighter assignments.
    /// Attach this to any active GameObject (or let it auto-find the GridController).
    /// It runs once in Awake and logs any cells that don't have Reachable/Path highlighters.
    /// </summary>
    public class CellHighlighterChecker : MonoBehaviour
    {
        [SerializeField] private UnityGridController _gridController;

        private void Awake()
        {
            if (_gridController == null)
            {
                _gridController = Object.FindFirstObjectByType<UnityGridController>();
            }

            if (_gridController == null)
            {
                Debug.LogWarning("CellHighlighterChecker: No UnityGridController found in scene.");
                return;
            }

            var cellManager = _gridController.CellManager as UnityCellManager;
            if (cellManager == null)
            {
                Debug.LogWarning("CellHighlighterChecker: GridController has no UnityCellManager assigned.");
                return;
            }

            var cells = cellManager.GetCells().ToList();
            int missingReachable = 0;
            int missingPath = 0;

            foreach (var c in cells)
            {
                var cellObj = c as Cell;
                if (cellObj == null) continue;

                // Use reflection to inspect private highlighter lists
                var fieldReachable = typeof(Cell).GetField("_markAsReachableFn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var fieldPath = typeof(Cell).GetField("_markAsPathFn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                var reachableList = fieldReachable?.GetValue(cellObj) as System.Collections.IEnumerable;
                var pathList = fieldPath?.GetValue(cellObj) as System.Collections.IEnumerable;

                bool hasReachable = reachableList != null && reachableList.GetEnumerator().MoveNext();
                bool hasPath = pathList != null && pathList.GetEnumerator().MoveNext();

                if (!hasReachable)
                {
                    missingReachable++;
                    Debug.LogWarning($"CellHighlighterChecker: Cell {cellObj.name} at {cellObj.GridCoordinates} has NO Reachable highlighter.", cellObj as Object);
                }
                if (!hasPath)
                {
                    missingPath++;
                    Debug.LogWarning($"CellHighlighterChecker: Cell {cellObj.name} at {cellObj.GridCoordinates} has NO Path highlighter.", cellObj as Object);
                }
            }

            Debug.Log($"CellHighlighterChecker: Scanned {cells.Count} cells. Missing Reachable={missingReachable}, Missing Path={missingPath}.");
        }
    }
}
