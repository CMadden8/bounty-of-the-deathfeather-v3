#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TurnBasedStrategyFramework.Unity.Units;
using TurnBasedStrategyFramework.Unity.Cells;
using System.Linq;
using System.Collections.Generic;

namespace CombatPOC.Editor
{
    /// <summary>
    /// Window-based utility to reposition units to cells.
    /// Open via Tools → Combat POC → Reposition Unit Window
    /// </summary>
    public class RepositionUnitWindow : EditorWindow
    {
        private Unit _selectedUnit;
        private Cell _targetCell;
        private List<Cell> _availableCells = new List<Cell>();
        private Vector2 _scrollPosition;
        private string _searchFilter = "";

        [MenuItem("Tools/Combat POC/Reposition Unit Window")]
        public static void ShowWindow()
        {
            GetWindow<RepositionUnitWindow>("Reposition Unit");
        }

        // Also add a Window menu entry to make it easy to find
        [MenuItem("Window/Combat POC/Reposition Unit Window")]
        public static void ShowWindowFromWindowMenu()
        {
            ShowWindow();
        }

        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInit()
        {
            Debug.Log("CombatPOC: RepositionUnitWindow editor script loaded.");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Reposition Unit to Cell", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Unit selection
            EditorGUILayout.LabelField("1. Select Unit:", EditorStyles.boldLabel);
            var newUnit = EditorGUILayout.ObjectField("Unit", _selectedUnit, typeof(Unit), true) as Unit;
            if (newUnit != _selectedUnit)
            {
                _selectedUnit = newUnit;
                RefreshCells();
            }

            if (_selectedUnit == null)
            {
                EditorGUILayout.HelpBox("Select a Unit from the scene hierarchy or drag it into the Unit field above.", MessageType.Info);
                return;
            }

            // Show current position
            if (_selectedUnit.CurrentCell is Cell currentCell)
            {
                EditorGUILayout.LabelField($"Current Position: {currentCell.name} at ({currentCell.GridCoordinates.x}, {currentCell.GridCoordinates.y})");
            }
            else
            {
                EditorGUILayout.LabelField("Current Position: None");
            }

            EditorGUILayout.Space();

            // Cell selection
            EditorGUILayout.LabelField("2. Select Target Cell:", EditorStyles.boldLabel);
            
            if (_availableCells.Count == 0)
            {
                if (GUILayout.Button("Refresh Cell List"))
                {
                    RefreshCells();
                }
                EditorGUILayout.HelpBox("No cells found. Click 'Refresh Cell List' or ensure cells are painted in the scene.", MessageType.Warning);
                return;
            }

            // Search filter
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            EditorGUILayout.EndHorizontal();

            // Cell list
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
            
            foreach (var cell in _availableCells)
            {
                string cellLabel = $"{cell.name} at ({cell.GridCoordinates.x}, {cell.GridCoordinates.y})";
                
                // Apply search filter
                if (!string.IsNullOrEmpty(_searchFilter) && !cellLabel.ToLower().Contains(_searchFilter.ToLower()))
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                
                // Show occupied status
                if (cell.IsTaken)
                {
                    var occupant = cell.CurrentUnits.FirstOrDefault();
                    string occupantName = (occupant as Unit)?.name ?? "unknown";
                    cellLabel += $" [Occupied by {occupantName}]";
                    GUI.color = Color.yellow;
                }
                
                if (GUILayout.Button(cellLabel))
                {
                    _targetCell = cell;
                    MoveUnitToCell(_selectedUnit, _targetCell);
                }
                
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Refresh Cell List", GUILayout.Height(30)))
            {
                RefreshCells();
            }
        }

        private void OnFocus()
        {
            // Auto-select unit from selection
            if (Selection.activeGameObject != null)
            {
                var unit = Selection.activeGameObject.GetComponent<Unit>();
                if (unit != null && unit != _selectedUnit)
                {
                    _selectedUnit = unit;
                    RefreshCells();
                }
            }
        }

        private void RefreshCells()
        {
            var cellManager = FindObjectOfType<RegularCellManager>();
            if (cellManager == null)
            {
                _availableCells.Clear();
                Debug.LogWarning("No RegularCellManager found in scene.");
                return;
            }

            _availableCells = cellManager.GetComponentsInChildren<Cell>()
                .OrderBy(c => c.GridCoordinates.x)
                .ThenBy(c => c.GridCoordinates.y)
                .ToList();
            
            Debug.Log($"Found {_availableCells.Count} cells in scene.");
        }

        private void MoveUnitToCell(Unit unit, Cell targetCell)
        {
            if (unit == null || targetCell == null) return;

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
            
            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            
            // Refresh the window
            Repaint();
        }
    }
}
#endif
