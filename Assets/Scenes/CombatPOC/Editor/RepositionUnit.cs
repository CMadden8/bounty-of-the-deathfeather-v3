#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TurnBasedStrategyFramework.Unity.Units;
using TurnBasedStrategyFramework.Unity.Cells;
using System.Linq;

namespace CombatPOC.Editor
{
    /// <summary>
    /// Context menu utility to reposition a selected unit to a target cell in edit mode.
    /// Right-click a unit in the hierarchy and choose "Reposition Unit to Cell".
    /// </summary>
    public static class RepositionUnit
    {
        [MenuItem("GameObject/Combat POC/Reposition Unit to Cell", false, 0)]
        private static void RepositionUnitToCell(MenuCommand command)
        {
            var unit = Selection.activeGameObject?.GetComponent<Unit>();
            if (unit == null)
            {
                EditorUtility.DisplayDialog("Reposition Unit", "Please select a Unit in the hierarchy.", "OK");
                return;
            }

            var cellManager = Object.FindFirstObjectByType<RegularCellManager>();
            if (cellManager == null)
            {
                EditorUtility.DisplayDialog("Reposition Unit", "No RegularCellManager found in scene.", "OK");
                return;
            }

            var cells = cellManager.GetComponentsInChildren<Cell>().OrderBy(c => c.GridCoordinates.x).ThenBy(c => c.GridCoordinates.y).ToList();
            if (cells.Count == 0)
            {
                EditorUtility.DisplayDialog("Reposition Unit", "No cells found. Paint some cells first.", "OK");
                return;
            }

            // Build menu with cell names and coordinates
            var menu = new GenericMenu();
            foreach (var cell in cells)
            {
                string label = $"{cell.name} at ({cell.GridCoordinates.x}, {cell.GridCoordinates.y})";
                if (cell.IsTaken)
                {
                    var occupant = cell.CurrentUnits.FirstOrDefault();
                    string occupantName = (occupant as Unit)?.name ?? "unknown";
                    label += $" [Occupied by {occupantName}]";
                }
                
                // Capture cell in closure properly
                Cell targetCell = cell;
                menu.AddItem(new GUIContent(label), false, () => MoveUnitToCell(unit, targetCell));
            }
            
            // Show menu at mouse position
            menu.ShowAsContext();
        }

        private static void MoveUnitToCell(Unit unit, Cell targetCell)
        {
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();

            // Clear old cell
            if (unit.CurrentCell is Cell oldCell)
            {
                Undo.RecordObject(oldCell, "Clear old cell");
                oldCell.IsTaken = false;
                oldCell.CurrentUnits.Remove(unit);
                EditorUtility.SetDirty(oldCell);
            }

            // Clear target cell if occupied
            if (targetCell.IsTaken)
            {
                Undo.RecordObject(targetCell, "Clear target cell");
                foreach (var occupant in targetCell.CurrentUnits.ToList())
                {
                    targetCell.CurrentUnits.Remove(occupant);
                    if (occupant is Unit occupantUnit)
                    {
                        Undo.RecordObject(occupantUnit, "Clear occupant CurrentCell");
                        occupantUnit.CurrentCell = null;
                        EditorUtility.SetDirty(occupantUnit);
                    }
                }
                targetCell.IsTaken = false;
            }

            // Move unit to target cell
            Undo.RecordObject(unit, "Reposition unit");
            Undo.RecordObject(targetCell, "Update target cell");
            
            unit.CurrentCell = targetCell;
            targetCell.IsTaken = true;
            targetCell.CurrentUnits.Add(unit);

            // Position unit at cell center with vertical offset
            Vector3 cellCenter = targetCell.transform.position + new Vector3(
                targetCell.CellDimensions.x * 0.5f,
                0,
                targetCell.CellDimensions.z * 0.5f
            );
            unit.transform.position = cellCenter + Vector3.up * 0.55f;

            EditorUtility.SetDirty(unit);
            EditorUtility.SetDirty(targetCell);
            
            Undo.CollapseUndoOperations(undoGroup);
            
            Debug.Log($"Repositioned {unit.name} to {targetCell.name} at ({targetCell.GridCoordinates.x}, {targetCell.GridCoordinates.y})");
        }

        [MenuItem("GameObject/Combat POC/Reposition Unit to Cell", true)]
        private static bool ValidateRepositionUnitToCell()
        {
            return Selection.activeGameObject?.GetComponent<Unit>() != null;
        }
    }
}
#endif
