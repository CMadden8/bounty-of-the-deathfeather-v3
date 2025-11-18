#if UNITY_EDITOR
using System.Linq;
using CombatPOC.Units;
using TurnBasedStrategyFramework.Unity.Cells;
using TurnBasedStrategyFramework.Unity.Controllers;
using TurnBasedStrategyFramework.Unity.Players;
using TurnBasedStrategyFramework.Unity.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CombatPOC.Editor
{
    /// <summary>
    /// Provides a one-click menu action to build the minimal MVP scene setup:
    /// - ensures required managers exist and are assigned to the grid controller
    /// - creates two placeholder players (Player 0 = Human, Player 1 = Human)
    /// - spawns two simple units and pins them to the first two painted cells
    /// </summary>
    public static class MinimalCombatSceneBuilder
    {
        private const string MenuPath = "Tools/Combat POC/Build Minimal MVP Setup";

        [MenuItem(MenuPath)]
        public static void Build()
        {
            var controller = Object.FindObjectOfType<UnityGridController>();
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Minimal MVP Setup", "No UnityGridController found in the open scene.", "OK");
                return;
            }

            var cellManager = Object.FindObjectOfType<RegularCellManager>();
            if (cellManager == null)
            {
                EditorUtility.DisplayDialog("Minimal MVP Setup", "No RegularCellManager found. Create/paint at least two cells first.", "OK");
                return;
            }

            var availableCells = cellManager.GetComponentsInChildren<Cell>().ToList();
            if (availableCells.Count < 2)
            {
                EditorUtility.DisplayDialog("Minimal MVP Setup", "Need at least two painted cells to position units.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();

            var playerManager = EnsureChildComponent<UnityPlayerManager>(controller.transform, "PlayerManager");
            var unitManager = EnsureChildComponent<UnityUnitManager>(controller.transform, "UnitManager");
            var turnResolver = EnsureChildComponent<SubsequentTurnResolver>(controller.transform, "TurnResolver");

            controller.CellManager = cellManager;
            controller.PlayerManager = playerManager;
            controller.UnitManager = unitManager;
            controller.TurnResolver = turnResolver;

            EnsurePlayer(playerManager.transform, "Player_0", 0, typeof(HumanPlayer));
            EnsurePlayer(playerManager.transform, "Player_1", 1, typeof(HumanPlayer));

            EnsureUnit(unitManager.transform, "Unit_Player", 0, availableCells[0]);
            EnsureUnit(unitManager.transform, "Unit_Enemy", 1, availableCells[1]);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(playerManager);
            EditorUtility.SetDirty(unitManager);
            EditorUtility.SetDirty(turnResolver.gameObject);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("Combat POC: Minimal MVP setup complete.");
        }

        private static T EnsureChildComponent<T>(Transform parent, string childName) where T : Component
        {
            var existing = parent.GetComponentsInChildren<T>(true).FirstOrDefault();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(childName, typeof(T));
            Undo.RegisterCreatedObjectUndo(go, "Create " + childName);
            go.transform.SetParent(parent, false);
            return go.GetComponent<T>();
        }

        private static Player EnsurePlayer(Transform parent, string name, int playerNumber, System.Type playerType)
        {
            var existing = parent.GetComponentsInChildren<Player>(true)
                                  .FirstOrDefault(p => p.PlayerNumber == playerNumber);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            var player = go.AddComponent(playerType) as Player;
            player.PlayerNumber = playerNumber;
            EditorUtility.SetDirty(player);
            return player;
        }

        private static SimpleUnit EnsureUnit(Transform parent, string name, int playerNumber, Cell targetCell)
        {
            var existing = parent.GetComponentsInChildren<SimpleUnit>(true)
                                 .FirstOrDefault(u => u.PlayerNumber == playerNumber);
            if (existing != null)
            {
                SnapUnitToCell(existing, targetCell);
                return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * 0.8f;

            var simpleUnit = go.AddComponent<SimpleUnit>();
            simpleUnit.PlayerNumber = playerNumber;
            SnapUnitToCell(simpleUnit, targetCell);
            EditorUtility.SetDirty(simpleUnit);

            return simpleUnit;
        }

        private static void SnapUnitToCell(SimpleUnit unit, Cell cell)
        {
            if (cell == null || unit == null)
            {
                return;
            }

            unit.CurrentCell = cell;
            cell.IsTaken = true;
            unit.transform.position = cell.transform.position + Vector3.up * 0.55f;
        }
    }
}
#endif
