using UnityEngine;
using TurnBasedStrategyFramework.Unity.Controllers;
using TurnBasedStrategyFramework.Unity.Cells;

namespace BountyOfTheDeathfeather.Debugging
{
    /// <summary>
    /// Debug helper to diagnose path highlighting issues.
    /// Attach to any GameObject in the scene.
    /// </summary>
    public class PathHighlightDebugger : MonoBehaviour
    {
        [SerializeField] private UnityGridController _gridController;
        [SerializeField] private bool _logCellEvents = true;

        private void Start()
        {
            if (_gridController == null)
            {
                _gridController = Object.FindFirstObjectByType<UnityGridController>();
            }

            if (_gridController == null)
            {
                Debug.LogWarning("PathHighlightDebugger: No GridController found");
                return;
            }

            var cellManager = _gridController.CellManager as UnityCellManager;
            if (cellManager == null)
            {
                Debug.LogWarning("PathHighlightDebugger: No UnityCellManager found");
                return;
            }

            // Subscribe to cell events
            if (_logCellEvents)
            {
                foreach (var cell in cellManager.GetCells())
                {
                    if (cell != null)
                    {
                        cell.CellHighlighted += OnCellHighlighted;
                        cell.CellDehighlighted += OnCellDehighlighted;
                    }
                }
                Debug.Log("PathHighlightDebugger: Subscribed to cell highlight events");
            }
        }

        private void OnCellHighlighted(TurnBasedStrategyFramework.Common.Cells.ICell cell)
        {
            var cellObj = cell as Cell;
            if (cellObj != null)
            {
                Debug.Log($"[PathDebug] Cell highlighted: {cellObj.name} at {cell.GridCoordinates}");
            }
        }

        private void OnCellDehighlighted(TurnBasedStrategyFramework.Common.Cells.ICell cell)
        {
            var cellObj = cell as Cell;
            if (cellObj != null)
            {
                Debug.Log($"[PathDebug] Cell dehighlighted: {cellObj.name} at {cell.GridCoordinates}");
            }
        }

        private void OnDestroy()
        {
            if (_gridController != null)
            {
                var cellManager = _gridController.CellManager as UnityCellManager;
                if (cellManager != null)
                {
                    foreach (var cell in cellManager.GetCells())
                    {
                        if (cell != null)
                        {
                            cell.CellHighlighted -= OnCellHighlighted;
                            cell.CellDehighlighted -= OnCellDehighlighted;
                        }
                    }
                }
            }
        }
    }
}
